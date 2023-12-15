namespace Colosoft.EventBus.RabbitMQ
{
    internal class RabbitMQIntegrationEventContext : IIntegrationEventContext
    {
        public RabbitMQIntegrationEventContext(global::RabbitMQ.Client.Events.BasicDeliverEventArgs deliverEventArgs)
        {
            this.Headers = deliverEventArgs.BasicProperties.Headers ?? new Dictionary<string, object>();
        }

        public IDictionary<string, object> Headers { get; }
    }
}
