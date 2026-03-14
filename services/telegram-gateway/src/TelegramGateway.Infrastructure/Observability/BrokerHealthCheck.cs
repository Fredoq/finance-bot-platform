using Microsoft.Extensions.Diagnostics.HealthChecks;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Infrastructure.Observability;

internal sealed class BrokerHealthCheck(IBrokerState state) : IHealthCheck
{
    /// <summary>
    /// Checks whether the broker is ready to serve traffic.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The broker health status.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await state.Ready(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Broker is unavailable");
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Broker is unavailable", error);
        }
    }
}
