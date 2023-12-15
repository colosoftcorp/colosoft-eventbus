namespace Colosoft.EventBus.RabbitMQ.Test
{
    internal class UserHeaderBehavior<TIntegrationEvent> : IPipelineHandlerBehavior<TIntegrationEvent>
        where TIntegrationEvent : IntegrationEvent
    {
        private readonly UserContext userContext;

        public UserHeaderBehavior(UserContext userContext)
        {
            this.userContext = userContext;
        }

        public Task Handle(
            TIntegrationEvent @event,
            IIntegrationEventContext context,
            IntegrationEventHandlerDelegate next,
            CancellationToken cancellationToken)
        {
            if (context.Headers.TryGetValue("userId", out var value) && int.TryParse(value.ToString(), out var userId))
            {
                this.userContext.UserId = userId;
            }

            return next(cancellationToken);
        }
    }
}
