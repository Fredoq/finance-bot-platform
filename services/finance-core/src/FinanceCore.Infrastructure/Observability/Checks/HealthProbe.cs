using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceCore.Infrastructure.Observability.Checks;

internal static class HealthProbe
{
    internal static async Task<HealthCheckResult> Run(string error, Func<CancellationToken, ValueTask<bool>> probe, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentNullException.ThrowIfNull(probe);
        try
        {
            return await probe(token) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(error);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception item)
        {
            return HealthCheckResult.Unhealthy(error, item);
        }
    }
    internal static async Task<HealthCheckResult> Run(string error, Func<CancellationToken, ValueTask> probe, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentNullException.ThrowIfNull(probe);
        try
        {
            await probe(token);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception item)
        {
            return HealthCheckResult.Unhealthy(error, item);
        }
    }
}
