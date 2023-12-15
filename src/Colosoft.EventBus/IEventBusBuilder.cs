using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.EventBus
{
    public interface IEventBusBuilder
    {
        public IServiceCollection Services { get; }
    }
}
