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
        Assert.Equal(1, await Number("select count(*) from information_schema.columns where table_schema = 'finance' and table_name = 'user_account' and column_name = 'time_zone' and is_nullable = 'NO'"));
        Assert.Equal(1, await Number("select count(*) from information_schema.tables where table_schema = 'finance' and table_name = 'transaction_entry'"));
        Assert.Equal(1, await Number("select count(distinct trigger_name) from information_schema.triggers where trigger_schema = 'finance' and event_object_table = 'account_transfer' and trigger_name = 'trg_account_transfer_integrity'"));
        Assert.Equal(8, await Number("select count(*) from finance.category where scope = 'system' and kind = 'expense'"));
        Assert.Equal(8, await Number("select count(*) from finance.category where scope = 'system' and kind = 'income'"));
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
    /// Verifies that startup repairs the expense schema when the baseline script is already journaled.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Repairs the expense schema after a journaled baseline startup")]
    public async Task Repairs_migration()
    {
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-repair-a")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        await Execute("drop table if exists finance.transaction_entry cascade; drop table if exists finance.category cascade");
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-repair-b")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        Assert.Equal(1, await Number("select count(*) from finance.schema_journal"));
        Assert.Equal(1, await Number("select count(*) from information_schema.tables where table_schema = 'finance' and table_name = 'category'"));
        Assert.Equal(1, await Number("select count(*) from information_schema.tables where table_schema = 'finance' and table_name = 'transaction_entry'"));
        Assert.Equal(8, await Number("select count(*) from finance.category where scope = 'system' and kind = 'expense'"));
        Assert.Equal(8, await Number("select count(*) from finance.category where scope = 'system' and kind = 'income'"));
    }
    /// <summary>
    /// Verifies that startup repairs the user time zone column after the baseline was already journaled.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Repairs the user time zone column after a journaled baseline startup")]
    public async Task Repairs_time_zone()
    {
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-zone-a")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        await Execute("""
                      alter table finance.user_account drop column if exists time_zone;
                      insert into finance.user_account(id, actor_key, name, locale, created_utc, updated_utc)
                      values ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, 'actor-zone-repair', 'Alex', 'en', '2026-01-01 00:00:00+00'::timestamptz, '2026-01-01 00:00:00+00'::timestamptz);
                      """);
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-zone-b")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        Assert.Equal(1, await Number("select count(*) from information_schema.columns where table_schema = 'finance' and table_name = 'user_account' and column_name = 'time_zone' and is_nullable = 'NO'"));
        Assert.Equal(1, await Number("select count(*) from finance.user_account where actor_key = 'actor-zone-repair' and time_zone = 'Etc/UTC'"));
    }
    /// <summary>
    /// Verifies that startup restores a drifted system category code after the baseline was already journaled.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Repairs a drifted income category after a journaled baseline startup")]
    public async Task Repairs_catalog()
    {
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-catalog-a")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        await Execute("""
                      update finance.category
                      set code = 'salary-drift', name = 'Salary Drift'
                      where id = '99999999-9999-9999-9999-999999999991'::uuid;
                      insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc)
                      values ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, 'income', 'system', null, 'salary', 'Salary Copy', '2026-01-01 00:00:00+00'::timestamptz, '2026-01-01 00:00:00+00'::timestamptz);
                      """);
        await using (var host = new CoreApiFactory(Settings("finance-core-migration-catalog-b")))
        {
            using HttpClient client = host.CreateClient();
            await Ready(client);
        }
        Assert.Equal(1, await Number("select count(*) from finance.category where id = '99999999-9999-9999-9999-999999999991'::uuid and kind = 'income' and code = 'salary' and name = 'Salary'"));
        Assert.Equal(0, await Number("select count(*) from finance.category where id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid"));
        Assert.Equal(0, await Number("select count(*) from finance.category where code = 'salary-drift'"));
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
