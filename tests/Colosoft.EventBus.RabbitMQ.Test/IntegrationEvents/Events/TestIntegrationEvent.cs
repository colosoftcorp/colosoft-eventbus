namespace Colosoft.EventBus.RabbitMQ.Test.IntegrationEvents.Events
{
    internal record TestIntegrationEvent : IntegrationEvent
    {
        public string Message { get; set; }
    }
}
