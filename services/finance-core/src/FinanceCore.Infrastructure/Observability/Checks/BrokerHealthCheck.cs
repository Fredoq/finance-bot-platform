using FinanceCore.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceCore.Infrastructure.Observability.Checks;

internal sealed class BrokerHealthCheck : IHealthCheck
{
    private const string Error = "Broker is unavailable";
    private readonly IBrokerState state;
    public BrokerHealthCheck(IBrokerState state) => this.state = state ?? throw new ArgumentNullException(nameof(state));
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => HealthProbe.Run(Error, state.Ready, cancellationToken);
}
