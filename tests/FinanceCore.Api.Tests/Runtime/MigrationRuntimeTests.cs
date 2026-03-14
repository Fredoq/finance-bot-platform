using System.Net;
using FinanceCore.Api.Tests.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FinanceCore.Api.Tests;

/// <summary>
/// Covers finance core migration and readiness behavior.
/// </summary>
public sealed class MigrationRuntimeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer sql = new PostgreSqlBuilder("postgres:17").WithUsername("finance").WithPassword("finance").WithDatabase("finance_core").Build();
    private readonly RabbitMqContainer box = new RabbitMqBuilder("rabbitmq:management").WithUsername("finance").WithPassword("finance").Build();
    private string rabbit = string.Empty;
    private string postgres = string.Empty;
    /// <summary>
    /// Starts the external dependencies.
    /// </summary>
    /// <returns>A task that completes when startup finishes.</returns>
    public async Task InitializeAsync()
    {
        await sql.StartAsync();
        await box.StartAsync();
        rabbit = box.GetConnectionString();
        postgres = sql.GetConnectionString();
    }
    /// <summary>
    /// Stops the external dependencies.
    /// </summary>
    /// <returns>A task that completes when disposal finishes.</returns>
    public async Task DisposeAsync()
    {
        await box.DisposeAsync();
        await sql.DisposeAsync();
    }
    /// <summary>
    /// Verifies that the baseline migration creates the journal table and records one script.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Applies the baseline migration and records it in the journal")]
    public async Task Applies_migration()
    {
        await using var host = new CoreApiFactory(Note("finance-core-migration"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        Assert.Equal(1, await Number("select count(*) from finance.schema_journal"));
        Assert.Equal(1, await Number("select count(*) from information_schema.tables where table_schema = 'finance' and table_name = 'user_account'"));
    }
    /// <summary>
    /// Verifies that a repeated startup does not duplicate the baseline migration journal row.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Does not replay an applied migration on the next startup")]
    public async Task Deduplicates_migration()
    {
        await using (var host = new CoreApiFactory(Note("finance-core-migration-a")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        await using (var host = new CoreApiFactory(Note("finance-core-migration-b")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        Assert.Equal(1, await Number("select count(*) from finance.schema_journal"));
    }
    /// <summary>
    /// Verifies that the readiness endpoint reports healthy dependencies.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 for the readiness endpoint when PostgreSQL and RabbitMQ are ready")]
    public async Task Ready_endpoint()
    {
        await using var host = new CoreApiFactory(Note("finance-core-ready"));
        using HttpClient client = host.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    private static async Task Ready(HttpClient client)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!note.IsCancellationRequested)
        {
            HttpResponseMessage response = await client.GetAsync("/health/ready", note.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
            await Task.Delay(250, note.Token);
        }
    }
    private async Task<long> Number(string text)
    {
        await using NpgsqlConnection link = new(postgres);
        await link.OpenAsync();
        await using NpgsqlCommand note = new(text, link);
        return (long)(await note.ExecuteScalarAsync() ?? throw new InvalidOperationException("Scalar query failed"));
    }
    private Dictionary<string, string?> Note(string name)
    {
        var item = new Uri(rabbit);
        string[] data = item.UserInfo.Split(':', 2, StringSplitOptions.None);
        return new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = postgres,
            ["RabbitMq:Host"] = item.Host,
            ["RabbitMq:Port"] = item.Port.ToString(),
            ["RabbitMq:VirtualHost"] = Uri.UnescapeDataString(item.AbsolutePath),
            ["RabbitMq:Username"] = data.Length > 0 ? Uri.UnescapeDataString(data[0]) : string.Empty,
            ["RabbitMq:Password"] = data.Length > 1 ? Uri.UnescapeDataString(data[1]) : string.Empty,
            ["RabbitMq:Exchange"] = "finance.command",
            ["RabbitMq:Queue"] = "finance-core.command",
            ["RabbitMq:RetryQueue"] = "finance-core.command.retry",
            ["RabbitMq:DeadQueue"] = "finance-core.command.dead",
            ["RabbitMq:Client"] = name,
            ["RabbitMq:Prefetch"] = "16",
            ["RabbitMq:RetryDelaySeconds"] = "1",
            ["RabbitMq:MaxAttempts"] = "5"
        };
    }
}
