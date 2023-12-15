namespace Colosoft.EventBus
{
    public interface IPublishProperties
    {
        IDictionary<string, object> Headers { get; }
    }
}
