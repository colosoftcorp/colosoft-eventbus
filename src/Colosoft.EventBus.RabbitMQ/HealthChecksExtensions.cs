using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Colosoft.EventBus.RabbitMQ
{
    internal static class HealthChecksExtensions
    {
        public static void TryAddHealthCheck(this IHostApplicationBuilder builder, HealthCheckRegistration healthCheckRegistration)
        {
            var healthCheckRegistration2 = healthCheckRegistration;
            TryAddHealthCheck(builder, healthCheckRegistration2.Name, (healthChecksBuilder) =>
            {
                healthChecksBuilder.Add(healthCheckRegistration2);
            });
        }

        public static void TryAddHealthCheck(this IHostApplicationBuilder builder, string name, Action<IHealthChecksBuilder> addHealthCheck)
        {
            string healthCheckKey = "Colosoft.HealthChecks." + name;
            if (!builder.Properties.ContainsKey(healthCheckKey))
            {
                builder.Properties[healthCheckKey] = true;
                addHealthCheck(builder.Services.AddHealthChecks());
            }
        }
    }
}
