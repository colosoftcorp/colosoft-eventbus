namespace Colosoft.EventBus
{
    public delegate Task IntegrationEventHandlerDelegate(CancellationToken cancellationToken);

    public interface IPipelineHandlerBehavior<in TIntegrationEvent>
    {
        Task Handle(TIntegrationEvent @event, IIntegrationEventContext context, IntegrationEventHandlerDelegate next, CancellationToken cancellationToken);
    }
}
