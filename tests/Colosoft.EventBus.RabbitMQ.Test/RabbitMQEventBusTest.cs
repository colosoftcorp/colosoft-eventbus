using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Colosoft.EventBus.RabbitMQ.Test
{
    public class RabbitMQEventBusTest
    {
        private readonly IServiceProvider serviceProvider;

        public RabbitMQEventBusTest()
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddSingleton<MessageRepository>();
            builder.Services.AddScoped<UserContext>();

            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    { "ConnectionStrings:EventBus", "amqp://localhost" },
                    { "EventBus:ExchangeName", "eventbus-test" },
                    { "EventBus:SubscriptionClientName", "client-test" },
                    { "EventBus:RetryCount", "5" },
                });

            builder.AddRabbitMqEventBus(
                "EventBus",
                config =>
                {
                    config.AddOpenBehavior(typeof(UserHeaderBehavior<>));
                })
                .AddSubscription<IntegrationEvents.Events.TestIntegrationEvent, IntegrationEvents.EventHandling.TestIntegrationEventHandler>();

            var host = builder.Build();

            this.serviceProvider = host.Services;

            host.Start();

            Thread.Sleep(2000);
        }

        [Fact]
        public async Task Publish_Receive()
        {
            var eventBus = this.serviceProvider.GetRequiredService<IEventBus>();
            var messageRepository = this.serviceProvider.GetRequiredService<MessageRepository>();

            var userId = 123;
            var properties = new PublishProperties();
            properties.Headers.Add("userId", userId);

            await eventBus.PublishAsync(
                new IntegrationEvents.Events.TestIntegrationEvent
                {
                    Message = "Hello",
                },
                properties,
                default);

            await Task.Delay(2000);

            Assert.NotEmpty(messageRepository.UserMessages);
            Assert.True(messageRepository.UserMessages.TryGetValue(userId, out var userMessages));

            Assert.True(userMessages.Contains("Hello"));
        }
    }
}