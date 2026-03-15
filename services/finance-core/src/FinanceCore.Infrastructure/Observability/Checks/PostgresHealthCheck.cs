using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace FinanceCore.Infrastructure.Observability.Checks;

internal sealed class PostgresHealthCheck : IHealthCheck
{
    private const string Error = "Postgres is unavailable";
    private readonly NpgsqlDataSource data;
    public PostgresHealthCheck(NpgsqlDataSource data) => this.data = data ?? throw new ArgumentNullException(nameof(data));
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => HealthProbe.Run(Error, Run, cancellationToken);
    private async ValueTask Run(CancellationToken token)
    {
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand ping = new("select 1", link);
        _ = await ping.ExecuteScalarAsync(token);
    }
}
