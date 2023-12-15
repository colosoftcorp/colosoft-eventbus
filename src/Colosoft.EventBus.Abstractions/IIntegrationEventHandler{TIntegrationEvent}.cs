namespace Colosoft.EventBus
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event, IIntegrationEventContext context, CancellationToken cancellationToken);

        Task IIntegrationEventHandler.Handle(IntegrationEvent @event, IIntegrationEventContext context, CancellationToken cancellationToken) =>
            this.Handle((TIntegrationEvent)@event, context, cancellationToken);
    }
}