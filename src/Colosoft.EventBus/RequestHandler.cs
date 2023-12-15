using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.EventBus
{
    internal class RequestHandler<TRequest> : RequestHandlerBase
        where TRequest : IntegrationEvent
    {
        public override Task Handle(
            IntegrationEvent request,
            IIntegrationEventContext context,
            IServiceProvider serviceProvider,
            Func<IntegrationEvent, IIntegrationEventContext, CancellationToken, Task> handler,
            CancellationToken cancellationToken)
        {
            async Task Handler(CancellationToken cancellationToken1)
            {
                await handler(request, context, cancellationToken1);
            }

            IntegrationEventHandlerDelegate handler1 = Handler;

            var result = serviceProvider
                .GetServices<IPipelineHandlerBehavior<TRequest>>()
                .Reverse()
                .Aggregate(
                    handler1,
                    (next, pipeline) => cancellationToken1 => pipeline.Handle((TRequest)request, context, next, cancellationToken1));

            return result(cancellationToken);
        }
    }
}
