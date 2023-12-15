namespace Colosoft.EventBus.RabbitMQ
{
    public class EventBusOptions
    {
        public string ConnectionName { get; set; }

        public string ExchangeName { get; set; }

        public string SubscriptionClientName { get; set; }

        public int RetryCount { get; set; } = 10;
    }
}