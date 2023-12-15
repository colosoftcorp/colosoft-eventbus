using Colosoft.EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.EventBus
{
    public static class EventBusConfigurationExtensions
    {
        public static IEventBusConfiguration AddBehavior<TImplementationType>(
            this IEventBusConfiguration configuration,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            return configuration.AddBehavior(typeof(TImplementationType), serviceLifetime);
        }

        public static IEventBusConfiguration AddBehavior(
            this IEventBusConfiguration configuration,
            Type implementationType,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (implementationType is null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (!(configuration is EventBusConfiguration configuration2))
            {
                return configuration;
            }

            var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineHandlerBehavior<>)).ToList();

            if (implementedGenericInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineHandlerBehavior<>).FullName}");
            }

            foreach (var implementedBehaviorType in implementedGenericInterfaces)
            {
                configuration2.BehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
            }

            return configuration;
        }

        public static IEventBusConfiguration AddOpenBehavior(
            this IEventBusConfiguration configuration,
            Type openBehaviorType,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (!(configuration is EventBusConfiguration configuration2))
            {
                return configuration;
            }

            if (!openBehaviorType.IsGenericType)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
            }

            var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
            var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineHandlerBehavior<>)));

            if (implementedOpenBehaviorInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineHandlerBehavior<>).FullName}");
            }

            foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
            {
                configuration2.BehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
            }

            return configuration;
        }
    }
}
