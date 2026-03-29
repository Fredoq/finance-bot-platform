using DbUp;
using DbUp.Engine;
using FinanceCore.Infrastructure.Configuration.Postgres;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

internal sealed class MigrationBoot : IHostedService
{
    private const long key = 5_832_147_011;
    private const string SchemaSql = "create schema if not exists finance";
    private const string RepairSql = """
                                      create table if not exists finance.category
                                      (
                                          id uuid primary key,
                                          kind text not null,
                                          scope text not null,
                                          user_id uuid null references finance.user_account(id) on delete cascade,
                                          code text null,
                                          name text not null,
                                          created_utc timestamptz not null,
                                          updated_utc timestamptz not null
                                      );
                                      
                                      create table if not exists finance.transaction_entry
                                      (
                                          id uuid primary key,
                                          user_id uuid not null references finance.user_account(id) on delete cascade,
                                          account_id uuid not null references finance.account(id) on delete cascade,
                                          category_id uuid not null references finance.category(id) on delete cascade,
                                          kind text not null,
                                          amount numeric(19, 4) not null,
                                          occurred_utc timestamptz not null,
                                          created_utc timestamptz not null,
                                          updated_utc timestamptz not null
                                      );
                                      
                                      drop index if exists finance.ux_category_system_code;
                                      drop index if exists finance.ux_category_user_name;
                                      create index if not exists idx_category_user on finance.category(user_id) where user_id is not null;
                                      create unique index if not exists ux_category_system_code on finance.category(kind, code) where user_id is null;
                                      create unique index if not exists ux_category_user_name on finance.category(user_id, kind, lower(name)) where user_id is not null;
                                      create index if not exists idx_transaction_entry_user_occurred on finance.transaction_entry(user_id, occurred_utc desc);
                                      
                                      do $$
                                      begin
                                          if exists (select 1 from information_schema.tables where table_schema = 'finance' and table_name = 'transaction_entry') then
                                              alter table finance.transaction_entry drop constraint if exists transaction_entry_category_id_fkey;
                                              alter table finance.transaction_entry add constraint transaction_entry_category_id_fkey foreign key (category_id) references finance.category(id) on delete cascade;
                                          end if;
                                      end;
                                      $$;
                                      
                                      with seed(id, kind, code, name) as
                                      (
                                          values
                                              ('11111111-1111-1111-1111-111111111111', 'expense', 'food', 'Food'),
                                              ('22222222-2222-2222-2222-222222222222', 'expense', 'transport', 'Transport'),
                                              ('33333333-3333-3333-3333-333333333333', 'expense', 'home', 'Home'),
                                              ('44444444-4444-4444-4444-444444444444', 'expense', 'health', 'Health'),
                                              ('55555555-5555-5555-5555-555555555555', 'expense', 'shopping', 'Shopping'),
                                              ('66666666-6666-6666-6666-666666666666', 'expense', 'fun', 'Fun'),
                                              ('77777777-7777-7777-7777-777777777777', 'expense', 'bills', 'Bills'),
                                              ('88888888-8888-8888-8888-888888888888', 'expense', 'travel', 'Travel'),
                                              ('99999999-9999-9999-9999-999999999991', 'income', 'salary', 'Salary'),
                                              ('99999999-9999-9999-9999-999999999992', 'income', 'bonus', 'Bonus'),
                                              ('99999999-9999-9999-9999-999999999993', 'income', 'gift', 'Gift'),
                                              ('99999999-9999-9999-9999-999999999994', 'income', 'cashback', 'Cashback'),
                                              ('99999999-9999-9999-9999-999999999995', 'income', 'sale', 'Sale'),
                                              ('99999999-9999-9999-9999-999999999996', 'income', 'interest', 'Interest'),
                                              ('99999999-9999-9999-9999-999999999997', 'income', 'refund', 'Refund'),
                                              ('99999999-9999-9999-9999-999999999998', 'income', 'other', 'Other')
                                      ),
                                      meta(scope, created_utc) as
                                      (
                                          values ('system', '2026-01-01 00:00:00+00'::timestamptz)
                                      )
                                      insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc)
                                      select seed.id::uuid, seed.kind, meta.scope, null, seed.code, seed.name, meta.created_utc, meta.created_utc
                                      from seed
                                      cross join meta
                                      on conflict do nothing;
                                      """;
    private readonly IOptions<PostgresOptions> option;
    private readonly ILoggerFactory factory;
    private readonly ILogger<MigrationBoot> log;
    internal MigrationBoot(IOptions<PostgresOptions> option, ILoggerFactory factory, ILogger<MigrationBoot> log)
    {
        this.option = option ?? throw new ArgumentNullException(nameof(option));
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
    public Task StartAsync(CancellationToken cancellationToken) => StartupRetry.Run("Postgres migration warm-up failed on attempt {Attempt} of {Count}", Ensure, log, cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    private async ValueTask Ensure(CancellationToken token)
    {
        string text = option.Value.ConnectionString;
        await using NpgsqlConnection link = new(text);
        await link.OpenAsync(token);
        await Create(link, token);
        await Lock(link, token);
        UpgradeEngine item = DeployChanges.To.PostgresqlDatabase(text).JournalToPostgresqlTable("finance", "schema_journal").WithScriptsEmbeddedInAssembly(typeof(MigrationBoot).Assembly, Name).LogTo(new DbUpLog(factory.CreateLogger<DbUpLog>())).Build();
        try
        {
            DatabaseUpgradeResult data = item.PerformUpgrade();
            if (!data.Successful)
            {
                throw new InvalidOperationException("Postgres migration failed", data.Error);
            }
            if (await NeedsRepair(link, token))
            {
                await Repair(link, token);
            }
        }
        finally
        {
            await Unlock(link, CancellationToken.None);
        }
    }
    private static async ValueTask Create(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new(SchemaSql, link);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    private static bool Name(string text) => text.Contains(".Persistence.Postgres.Migrations.Scripts.", StringComparison.Ordinal) && text.EndsWith(".sql", StringComparison.Ordinal);
    private static async ValueTask<bool> NeedsRepair(NpgsqlConnection link, CancellationToken token)
    {
        if (!await Exists(link, "category", token) || !await Exists(link, "transaction_entry", token))
        {
            return true;
        }
        if (!await Cascade(link, token))
        {
            return true;
        }
        return await Seed(link, "expense", token) < 8 || await Seed(link, "income", token) < 8;
    }
    private static async ValueTask<bool> Exists(NpgsqlConnection link, string name, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select exists (select 1 from information_schema.tables where table_schema = 'finance' and table_name = @table_name)", link);
        note.Parameters.AddWithValue("table_name", name);
        object? data = await note.ExecuteScalarAsync(token);
        return data is bool state && state;
    }
    private static async ValueTask<bool> Cascade(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new("""
                                            select exists
                                            (
                                                select 1
                                                from information_schema.referential_constraints
                                                where constraint_schema = 'finance'
                                                  and constraint_name = 'transaction_entry_category_id_fkey'
                                                  and delete_rule = 'CASCADE'
                                            )
                                            """, link);
        object? data = await note.ExecuteScalarAsync(token);
        return data is bool state && state;
    }
    private static async ValueTask<long> Seed(NpgsqlConnection link, string kind, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select count(*) from finance.category where scope = 'system' and kind = @kind", link);
        note.Parameters.AddWithValue("kind", kind);
        object? data = await note.ExecuteScalarAsync(token);
        return data is long item ? item : 0L;
    }
    private static async ValueTask Repair(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new(RepairSql, link);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    private static async ValueTask Lock(NpgsqlConnection link, CancellationToken token)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var item = CancellationTokenSource.CreateLinkedTokenSource(token, note.Token);
        while (true)
        {
            try
            {
                await using NpgsqlCommand gate = new("select pg_try_advisory_lock(@key)", link);
                gate.Parameters.AddWithValue("key", key);
                object? data = await gate.ExecuteScalarAsync(item.Token);
                if (data is bool state && state)
                {
                    return;
                }
                await Task.Delay(250, item.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                throw new TimeoutException("Postgres migration lock timed out");
            }
        }
    }
    private static async ValueTask Unlock(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select pg_advisory_unlock(@key)", link);
        note.Parameters.AddWithValue("key", key);
        _ = await note.ExecuteScalarAsync(token);
    }
}
