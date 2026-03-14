using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace FinanceCore.Infrastructure.Observability.Checks;

internal sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource data;
    public PostgresHealthCheck(NpgsqlDataSource data) => this.data = data ?? throw new ArgumentNullException(nameof(data));
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using NpgsqlConnection link = await data.OpenConnectionAsync(cancellationToken);
            await using NpgsqlCommand ping = new("select 1", link);
            _ = await ping.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Postgres is unavailable", error);
        }
    }
}
