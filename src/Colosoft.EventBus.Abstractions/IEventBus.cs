namespace Colosoft.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync(
            IntegrationEvent @event,
            CancellationToken cancellationToken);

        Task PublishAsync(
            IntegrationEvent @event,
            IPublishProperties properties,
            CancellationToken cancellationToken);
    }
}
