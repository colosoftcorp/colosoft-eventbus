using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Colosoft.EventBus.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus, IDisposable, IHostedService
    {
        private readonly string exchangeName;
        private readonly ILogger<RabbitMQEventBus> logger;
        private readonly IServiceProvider serviceProvider;

        private readonly int retryCount;
        private readonly ActivitySource activitySource;
        private readonly EventBusSubscriptionInfo subscriptionInfo;
        private readonly string queueName;

        private IConnection rabbitMQConnection;
        private IModel consumerChannel;

        public RabbitMQEventBus(
            ILogger<RabbitMQEventBus> logger,
            IServiceProvider serviceProvider,
            ActivitySource activitySource,
            IOptions<EventBusOptions> options,
            IOptions<EventBusSubscriptionInfo> subscriptionOptions)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.activitySource = activitySource ?? new ActivitySource("event-bus");
            this.exchangeName = options.Value.ExchangeName;
            this.retryCount = options.Value.RetryCount;
            this.subscriptionInfo = subscriptionOptions.Value;
            this.queueName = options.Value.SubscriptionClientName;
        }

        private static void SetActivityContext(Activity activity, string routingKey, string operation)
        {
            if (activity is not null)
            {
                activity.SetTag("messaging.system", "rabbitmq");
                activity.SetTag("messaging.destination_kind", "queue");
                activity.SetTag("messaging.operation", operation);
                activity.SetTag("messaging.destination.name", routingKey);
                activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
            }
        }

        public Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken)
        {
            return this.PublishAsync(@event, new PublishProperties(), cancellationToken);
        }

        public Task PublishAsync(
            IntegrationEvent @event,
            IPublishProperties properties,
            CancellationToken cancellationToken)
        {
            var policy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(this.retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var routingKey = @event.GetType().Name;

            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                this.logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, routingKey);
            }

            using var channel = this.rabbitMQConnection?.CreateModel() ?? throw new InvalidOperationException("RabbitMQ connection is not open");

            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                this.logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);
            }

            channel.ExchangeDeclare(exchange: this.exchangeName, type: "direct");

            var body = this.SerializeMessage(@event);

            var activityName = $"{routingKey} publish";

            return policy.ExecuteAsync(
                (cancellationToken1) =>
                {
                    using var activity = this.activitySource.StartActivity(activityName, ActivityKind.Client);

                    var basicProperties = channel.CreateBasicProperties();

                    basicProperties.DeliveryMode = 2;

                    if (properties.Headers != null)
                    {
                        basicProperties.Headers = properties.Headers;
                    }

                    SetActivityContext(activity, routingKey, "publish");

                    if (this.logger.IsEnabled(LogLevel.Trace))
                    {
                        this.logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);
                    }

                    try
                    {
                        channel.BasicPublish(
                            exchange: this.exchangeName,
                            routingKey: routingKey,
                            mandatory: true,
                            basicProperties: basicProperties,
                            body: body);

                        return Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        activity.SetExceptionTags(ex);

                        throw;
                    }
                },
                cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.consumerChannel?.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
        {
            var activityName = $"{eventArgs.RoutingKey} receive";

            using var activity = this.activitySource.StartActivity(activityName, ActivityKind.Client);

            SetActivityContext(activity, eventArgs.RoutingKey, "receive");

            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                activity?.SetTag("message", message);

                if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                var context = new RabbitMQIntegrationEventContext(eventArgs);
                await this.ProcessEvent(eventName, message, context, default);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);

                activity.SetExceptionTags(ex);
            }

            this.consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private async Task ProcessEvent(
            string eventName,
            string message,
            RabbitMQIntegrationEventContext context,
            CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                this.logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
            }

            await using var scope = this.serviceProvider.CreateAsyncScope();

            if (!this.subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
            {
                this.logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
                return;
            }

            foreach (var handler in scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
            {
                var integrationEvent = this.DeserializeMessage(message, eventType);

                await IntegrationEventHandlerMediator.Handle(
                    integrationEvent,
                    context,
                    scope.ServiceProvider,
                    handler.Handle,
                    cancellationToken);
            }
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:RequiresUnreferencedCode",
            Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed .NET apps, ensures the JsonSerializer doesn't use Reflection.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
        private IntegrationEvent DeserializeMessage(string message, Type eventType)
        {
            return JsonSerializer.Deserialize(message, eventType, this.subscriptionInfo.JsonSerializerOptions) as IntegrationEvent;
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:RequiresUnreferencedCode",
            Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed .NET apps, ensures the JsonSerializer doesn't use Reflection.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
        private byte[] SerializeMessage(IntegrationEvent @event)
        {
            return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), this.subscriptionInfo.JsonSerializerOptions);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        this.logger.LogInformation("Starting RabbitMQ connection on a background thread");

                        this.rabbitMQConnection = this.serviceProvider.GetRequiredService<IConnection>();
                        if (!this.rabbitMQConnection.IsOpen)
                        {
                            return;
                        }

                        if (this.logger.IsEnabled(LogLevel.Trace))
                        {
                            this.logger.LogTrace("Creating RabbitMQ consumer channel");
                        }

                        this.consumerChannel = this.rabbitMQConnection.CreateModel();

                        this.consumerChannel.CallbackException += (sender, ea) =>
                        {
                            this.logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
                        };

                        this.consumerChannel.ExchangeDeclare(
                            exchange: this.exchangeName,
                            type: "direct");

                        this.consumerChannel.QueueDeclare(
                            queue: this.queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

                        if (this.logger.IsEnabled(LogLevel.Trace))
                        {
                            this.logger.LogTrace("Starting RabbitMQ basic consume");
                        }

                        var consumer = new AsyncEventingBasicConsumer(this.consumerChannel);

                        consumer.Received += this.OnMessageReceived;

                        this.consumerChannel.BasicConsume(
                            queue: this.queueName,
                            autoAck: false,
                            consumer: consumer);

                        foreach (var (eventName, _) in this.subscriptionInfo.EventTypes)
                        {
                            this.consumerChannel.QueueBind(
                                queue: this.queueName,
                                exchange: this.exchangeName,
                                routingKey: eventName);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Error starting RabbitMQ connection");
                    }
                },
                TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
