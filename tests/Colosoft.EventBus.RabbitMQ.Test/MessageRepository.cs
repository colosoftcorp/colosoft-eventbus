namespace Colosoft.EventBus.RabbitMQ.Test
{
    internal class MessageRepository
    {
        public Dictionary<int, IList<string>> UserMessages { get; } = new Dictionary<int, IList<string>>();
    }
}
