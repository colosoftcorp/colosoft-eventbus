using System.Collections.Concurrent;

namespace Colosoft.EventBus
{
    public static class IntegrationEventHandlerMediator
    {
        private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestHandlers = new ConcurrentDictionary<Type, RequestHandlerBase>();

        public static Task Handle(
            IntegrationEvent request,
            IIntegrationEventContext context,
            IServiceProvider serviceProvider,
            Func<IntegrationEvent, IIntegrationEventContext, CancellationToken, Task> source,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var requestType = request.GetType();

            var handler = RequestHandlers.GetOrAdd(requestType == typeof(IntegrationEvent) ? request.GetType() : requestType, requestType =>
            {
                var wrapperType = typeof(RequestHandler<>).MakeGenericType(requestType);
                var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
                return (RequestHandlerBase)wrapper;
            });

            return handler.Handle(request, context, serviceProvider, source, cancellationToken);
        }
    }
}
