using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.EventBus.RabbitMQ
{
    public class EventBusConfiguration : IEventBusConfiguration
    {
        public List<ServiceDescriptor> BehaviorsToRegister { get; } = new List<ServiceDescriptor>();
    }
}
