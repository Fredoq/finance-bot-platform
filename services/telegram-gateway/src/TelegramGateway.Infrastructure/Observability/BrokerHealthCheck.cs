using Microsoft.Extensions.Diagnostics.HealthChecks;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Infrastructure.Observability;

/// <summary>
/// Checks that the RabbitMQ transport is reachable and ready.
/// Example:
/// <code>
/// HealthCheckResult item = await check.CheckHealthAsync(context, cancellationToken);
/// </code>
/// </summary>
internal sealed class BrokerHealthCheck(IBrokerState state) : IHealthCheck
{
    /// <summary>
    /// Checks the broker readiness state.
    /// Example:
    /// <code>
    /// HealthCheckResult item = await check.CheckHealthAsync(context, cancellationToken);
    /// </code>
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await state.Ensure(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Broker is unavailable", error);
        }
    }
}
