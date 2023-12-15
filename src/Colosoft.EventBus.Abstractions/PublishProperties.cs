namespace Colosoft.EventBus
{
    public class PublishProperties : IPublishProperties
    {
        public IDictionary<string, object> Headers { get; } = new Dictionary<string, object>();
    }
}
