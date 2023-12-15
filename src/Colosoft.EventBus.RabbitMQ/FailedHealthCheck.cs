using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Colosoft.EventBus
{
    internal sealed class FailedHealthCheck : IHealthCheck
    {
        private readonly Exception ex;

        public FailedHealthCheck(Exception ex)
        {
            this.ex = ex;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, null, this.ex));
        }
    }
}