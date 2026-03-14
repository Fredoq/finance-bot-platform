using FinanceCore.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceCore.Infrastructure.Observability.Checks;

internal sealed class BrokerHealthCheck : IHealthCheck
{
    private readonly IBrokerState state;
    public BrokerHealthCheck(IBrokerState state) => this.state = state ?? throw new ArgumentNullException(nameof(state));
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await state.Ready(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Broker is unavailable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Broker is unavailable", error);
        }
    }
}
