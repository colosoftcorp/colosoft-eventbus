namespace Colosoft.EventBus
{
    public interface IIntegrationEventHandler
    {
        Task Handle(IntegrationEvent @event, IIntegrationEventContext context, CancellationToken cancellationToken);
    }
}