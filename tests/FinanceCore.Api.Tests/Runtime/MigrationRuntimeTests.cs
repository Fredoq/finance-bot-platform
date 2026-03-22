using System.Net;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Runtime;

/// <summary>
/// Covers finance core migration and readiness behavior.
/// </summary>
public sealed class MigrationRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the baseline migration creates the journal table and records one script.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Applies the baseline migration and records it in the journal")]
    public async Task Applies_migration()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-migration"));
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
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-a")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-b")))
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
        await using var host = new CoreApiFactory(Settings("finance-core-ready"));
        using HttpClient client = host.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
