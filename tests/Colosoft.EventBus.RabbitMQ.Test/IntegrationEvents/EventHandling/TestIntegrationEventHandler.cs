using Colosoft.EventBus.RabbitMQ.Test.IntegrationEvents.Events;

namespace Colosoft.EventBus.RabbitMQ.Test.IntegrationEvents.EventHandling
{
    internal class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        private readonly MessageRepository messageRepository;
        private readonly UserContext userContext;

        public TestIntegrationEventHandler(
            MessageRepository messageRepository,
            UserContext userContext)
        {
            this.messageRepository = messageRepository;
            this.userContext = userContext;
        }

        public Task Handle(TestIntegrationEvent @event, IIntegrationEventContext context, CancellationToken cancellationToken)
        {
            if (!this.messageRepository.UserMessages.TryGetValue(this.userContext.UserId, out var messages))
            {
                messages = new List<string>();
                this.messageRepository.UserMessages.TryAdd(this.userContext.UserId, messages);
            }

            messages.Add(@event.Message);

            return Task.CompletedTask;
        }
    }
}
