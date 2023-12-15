namespace Colosoft.EventBus
{
    internal abstract class RequestHandlerBase
    {
        public abstract Task Handle(
            IntegrationEvent request,
            IIntegrationEventContext context,
            IServiceProvider serviceProvider,
            Func<IntegrationEvent, IIntegrationEventContext, CancellationToken, Task> handler,
            CancellationToken cancellationToken);
    }
}
