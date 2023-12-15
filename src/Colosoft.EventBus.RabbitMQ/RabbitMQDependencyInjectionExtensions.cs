using Colosoft.EventBus.RabbitMQ;
using HealthChecks.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Net.Sockets;

namespace Colosoft.EventBus
{
    public static class RabbitMQDependencyInjectionExtensions
    {
        private const string SectionName = "EventBus";
        private const string DefaultConfigSectionName = "Colosoft:RabbitMQ:Client";

        private static readonly ActivitySource ClientActivitySource = new ActivitySource("Colosoft.RabbitMQ.Client");

        public static IEventBusBuilder AddRabbitMqEventBus(
            this IHostApplicationBuilder builder,
            string connectionName,
            Action<IEventBusConfiguration> configurator = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddRabbitMQ(connectionName, configureConnectionFactory: factory =>
            {
                ((ConnectionFactory)factory).DispatchConsumersAsync = true;
            });

            builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));

            builder.Services.AddSingleton<IEventBus>(serviderProvider =>
            {
                var logger = serviderProvider.GetRequiredService<ILogger<RabbitMQEventBus>>();
                var options = serviderProvider.GetRequiredService<IOptions<EventBusOptions>>();
                var subscriptionOptions = serviderProvider.GetRequiredService<IOptions<EventBusSubscriptionInfo>>();

                return new RabbitMQEventBus(logger, serviderProvider, null, options, subscriptionOptions);
            });
            builder.Services.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

            var configuration = new EventBusConfiguration();

            if (configurator != null)
            {
                configurator(configuration);
            }

            foreach (var serviceDescriptor in configuration.BehaviorsToRegister)
            {
                builder.Services.TryAddEnumerable(serviceDescriptor);
            }

            return new EventBusBuilder(builder.Services);
        }

        public static void AddRabbitMQ(
            this IHostApplicationBuilder builder,
            string connectionName,
            Action<RabbitMQClientSettings> configureSettings = null,
            Action<IConnectionFactory> configureConnectionFactory = null)
        {
            AddRabbitMQ(builder, DefaultConfigSectionName, configureSettings, configureConnectionFactory, connectionName, null);
        }

        public static void AddKeyedRabbitMQ(
            this IHostApplicationBuilder builder,
            string name,
            Action<RabbitMQClientSettings> configureSettings = null,
            Action<IConnectionFactory> configureConnectionFactory = null)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddRabbitMQ(builder, "Colosoft:RabbitMQ:Client:" + name, configureSettings, configureConnectionFactory, name, name);
        }

        private static void AddRabbitMQ(
            this IHostApplicationBuilder builder,
            string configurationSectionName,
            Action<RabbitMQClientSettings> configureSettings,
            Action<IConnectionFactory> configureConnectionFactory,
            string connectionName,
            object serviceKey)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configureConnectionFactory2 = configureConnectionFactory;
            var serviceKey2 = serviceKey;

            var configSection = builder.Configuration.GetSection(configurationSectionName);
            var settings = new RabbitMQClientSettings();
            configSection.Bind_RabbitMQClientSettings(settings);
            var connectionString = builder.Configuration.GetConnectionString(connectionName);
            if (connectionString != null)
            {
                settings.ConnectionString = connectionString;
            }

            configureSettings?.Invoke(settings);
            if (serviceKey2 == null)
            {
                builder.Services.AddSingleton(CreateConnectionFactory);
                builder.Services.AddSingleton((IServiceProvider sp) => CreateConnection(sp.GetRequiredService<IConnectionFactory>(), settings.MaxConnectRetryCount));
            }
            else
            {
                builder.Services.AddKeyedSingleton(serviceKey2, (IServiceProvider sp, object _) => CreateConnectionFactory(sp));
                builder.Services.AddKeyedSingleton(serviceKey2, (IServiceProvider sp, object key) => CreateConnection(sp.GetRequiredKeyedService<IConnectionFactory>(key), settings.MaxConnectRetryCount));
            }

            if (!settings.HealthChecks)
            {
                return;
            }

            builder.TryAddHealthCheck(
                new HealthCheckRegistration(
                    (serviceKey2 == null) ? "RabbitMQ.Client" : ("RabbitMQ.Client_" + connectionName),
                    (sp) =>
                    {
                        try
                        {
                            var val = new RabbitMQHealthCheckOptions();
                            val.Connection = (serviceKey2 == null) ? sp.GetRequiredService<IConnection>() : sp.GetRequiredKeyedService<IConnection>(serviceKey2);
                            return new RabbitMQHealthCheck(val);
                        }
                        catch (Exception ex)
                        {
                            return new FailedHealthCheck(ex);
                        }
                    },
                    null,
                    null));

            IConnectionFactory CreateConnectionFactory(IServiceProvider sp)
            {
                var connectionFactory = new ConnectionFactory();
                var section = configSection.GetSection("ConnectionFactory");
                section.Bind_ConnectionFactory(connectionFactory);
                var connectionString2 = settings.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString2))
                {
                    connectionFactory.Uri = new Uri(connectionString2);
                }

                configureConnectionFactory2?.Invoke(connectionFactory);
                return connectionFactory;
            }
        }

        private static IConnection CreateConnection(IConnectionFactory factory, int retryCount)
        {
            IConnectionFactory factory2 = factory;
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(retryCount, (int retryAttempt) => TimeSpan.FromSeconds(Math.Pow(2.0, retryAttempt)));

            using Activity activity = ClientActivitySource.StartActivity("rabbitmq connect", ActivityKind.Client);
            AddRabbitMQTags(activity);

            return policy.Execute(() =>
            {
                using Activity activity2 = ClientActivitySource.StartActivity("rabbitmq connect attempt", ActivityKind.Client);
                AddRabbitMQTags(activity2, "connect");

                try
                {
                    return factory2.CreateConnection();
                }
                catch (Exception ex)
                {
                    if (activity2 != null)
                    {
                        activity2.AddTag("exception.message", ex.Message);
                        activity2.AddTag("exception.stacktrace", ex.ToString());
                        activity2.AddTag("exception.type", ex.GetType().FullName);
                        activity2.SetStatus(ActivityStatusCode.Error);
                    }

                    throw;
                }
            });
        }

        private static void AddRabbitMQTags(Activity activity, string operation = null)
        {
            if (activity != null)
            {
                activity.AddTag("messaging.system", "rabbitmq");
                if (operation != null)
                {
                    activity.AddTag("messaging.operation", operation);
                }
            }
        }

        private sealed class EventBusBuilder : IEventBusBuilder
        {
            private readonly IServiceCollection services;

            public EventBusBuilder(IServiceCollection services)
            {
                this.services = services;
            }

            public IServiceCollection Services => this.services;
        }
    }
}
