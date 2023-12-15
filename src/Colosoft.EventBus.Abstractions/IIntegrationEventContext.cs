namespace Colosoft.EventBus
{
    public interface IIntegrationEventContext
    {
        IDictionary<string, object> Headers { get; }
    }
}
