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
    private const long Key = 5_832_147_011;
    private const string ExpenseKind = "expense";
    private const string IncomeKind = "income";
    private const string SchemaSql = "create schema if not exists finance";
    private const string RepairSql = """
                                      do $$
                                      begin
                                          if exists
                                          (
                                              select 1
                                              from information_schema.tables
                                              where table_schema = 'finance'
                                                and table_name = 'user_account'
                                          ) then
                                              alter table finance.user_account add column if not exists time_zone text;
                                              update finance.user_account
                                              set time_zone = 'Etc/UTC'
                                              where time_zone is null or btrim(time_zone) = '';
                                              alter table finance.user_account alter column time_zone set not null;
                                          end if;
                                      end;
                                      $$;

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

                                      create table if not exists finance.account_transfer
                                      (
                                          id uuid primary key,
                                          user_id uuid not null references finance.user_account(id) on delete cascade,
                                          source_account_id uuid not null references finance.account(id) on delete cascade,
                                          target_account_id uuid not null references finance.account(id) on delete cascade,
                                          currency_code text not null,
                                          amount numeric(19, 4) not null,
                                          occurred_utc timestamptz not null,
                                          created_utc timestamptz not null,
                                          updated_utc timestamptz not null,
                                          constraint ck_account_transfer_distinct_accounts check (source_account_id <> target_account_id),
                                          constraint ck_account_transfer_positive_amount check (amount > 0)
                                      );

                                      do $$
                                      begin
                                          if exists (select 1 from information_schema.tables where table_schema = 'finance' and table_name = 'account_transfer') then
                                              if not exists
                                              (
                                                  select 1
                                                  from pg_constraint item
                                                  join pg_class rel on rel.oid = item.conrelid
                                                  join pg_namespace space on space.oid = rel.relnamespace
                                                  where space.nspname = 'finance'
                                                    and rel.relname = 'account_transfer'
                                                    and item.conname = 'ck_account_transfer_distinct_accounts'
                                                    and pg_get_constraintdef(item.oid) = 'CHECK ((source_account_id <> target_account_id))'
                                              ) then
                                                  alter table finance.account_transfer drop constraint if exists ck_account_transfer_distinct_accounts;
                                                  alter table finance.account_transfer add constraint ck_account_transfer_distinct_accounts check (source_account_id <> target_account_id);
                                              end if;
                                              if not exists
                                              (
                                                  select 1
                                                  from pg_constraint item
                                                  join pg_class rel on rel.oid = item.conrelid
                                                  join pg_namespace space on space.oid = rel.relnamespace
                                                  where space.nspname = 'finance'
                                                    and rel.relname = 'account_transfer'
                                                    and item.conname = 'ck_account_transfer_positive_amount'
                                                    and pg_get_constraintdef(item.oid) = 'CHECK ((amount > (0)::numeric))'
                                              ) then
                                                  alter table finance.account_transfer drop constraint if exists ck_account_transfer_positive_amount;
                                                  alter table finance.account_transfer add constraint ck_account_transfer_positive_amount check (amount > 0);
                                              end if;
                                          end if;
                                      end;
                                      $$;

                                      create or replace function finance.fn_account_transfer_integrity()
                                      returns trigger
                                      language plpgsql
                                      as $$
                                      declare
                                          source_currency text;
                                          target_currency text;
                                      begin
                                          select item.currency_code
                                          into source_currency
                                          from finance.account item
                                          where item.id = new.source_account_id and item.user_id = new.user_id;
                                          if source_currency is null then
                                              raise exception 'Transfer source account is invalid for user';
                                          end if;
                                          select item.currency_code
                                          into target_currency
                                          from finance.account item
                                          where item.id = new.target_account_id and item.user_id = new.user_id;
                                          if target_currency is null then
                                              raise exception 'Transfer target account is invalid for user';
                                          end if;
                                          if source_currency <> target_currency then
                                              raise exception 'Transfer accounts must have same currency';
                                          end if;
                                          if new.currency_code <> source_currency then
                                              raise exception 'Transfer currency must match account currency';
                                          end if;
                                          return new;
                                      end;
                                      $$;

                                      drop trigger if exists trg_account_transfer_integrity on finance.account_transfer;
                                      create trigger trg_account_transfer_integrity
                                      before insert or update on finance.account_transfer
                                      for each row execute function finance.fn_account_transfer_integrity();

                                      drop index if exists finance.ux_category_system_code;
                                      drop index if exists finance.ux_category_user_name;
                                      create index if not exists idx_category_user on finance.category(user_id) where user_id is not null;
                                      create unique index if not exists ux_category_system_code on finance.category(kind, code) where user_id is null;
                                      create unique index if not exists ux_category_user_name on finance.category(user_id, kind, lower(name)) where user_id is not null;
                                      create index if not exists idx_transaction_entry_user_occurred on finance.transaction_entry(user_id, occurred_utc desc);
                                      create index if not exists idx_account_transfer_user_occurred on finance.account_transfer(user_id, occurred_utc desc);
                                      create index if not exists idx_account_transfer_source on finance.account_transfer(source_account_id);
                                      create index if not exists idx_account_transfer_target on finance.account_transfer(target_account_id);

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
                                      )
                                      update finance.transaction_entry item
                                      set category_id = seed.id::uuid
                                      from finance.category current
                                      join seed on seed.kind = current.kind and seed.code = current.code
                                      where current.scope = 'system'
                                        and current.id <> seed.id::uuid
                                        and item.category_id = current.id;

                                      with seed(id, kind, code) as
                                      (
                                          values
                                              ('11111111-1111-1111-1111-111111111111', 'expense', 'food'),
                                              ('22222222-2222-2222-2222-222222222222', 'expense', 'transport'),
                                              ('33333333-3333-3333-3333-333333333333', 'expense', 'home'),
                                              ('44444444-4444-4444-4444-444444444444', 'expense', 'health'),
                                              ('55555555-5555-5555-5555-555555555555', 'expense', 'shopping'),
                                              ('66666666-6666-6666-6666-666666666666', 'expense', 'fun'),
                                              ('77777777-7777-7777-7777-777777777777', 'expense', 'bills'),
                                              ('88888888-8888-8888-8888-888888888888', 'expense', 'travel'),
                                              ('99999999-9999-9999-9999-999999999991', 'income', 'salary'),
                                              ('99999999-9999-9999-9999-999999999992', 'income', 'bonus'),
                                              ('99999999-9999-9999-9999-999999999993', 'income', 'gift'),
                                              ('99999999-9999-9999-9999-999999999994', 'income', 'cashback'),
                                              ('99999999-9999-9999-9999-999999999995', 'income', 'sale'),
                                              ('99999999-9999-9999-9999-999999999996', 'income', 'interest'),
                                              ('99999999-9999-9999-9999-999999999997', 'income', 'refund'),
                                              ('99999999-9999-9999-9999-999999999998', 'income', 'other')
                                      )
                                      delete from finance.category current
                                      using seed
                                      where current.scope = 'system'
                                        and current.kind = seed.kind
                                        and current.code = seed.code
                                        and current.id <> seed.id::uuid;

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
                                      on conflict (id) do update
                                      set kind = excluded.kind,
                                          scope = excluded.scope,
                                          user_id = excluded.user_id,
                                          code = excluded.code,
                                          name = excluded.name,
                                          updated_utc = excluded.updated_utc;
                                      """;
    private static readonly IReadOnlyDictionary<string, string> Expense = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["food"] = "11111111-1111-1111-1111-111111111111",
        ["transport"] = "22222222-2222-2222-2222-222222222222",
        ["home"] = "33333333-3333-3333-3333-333333333333",
        ["health"] = "44444444-4444-4444-4444-444444444444",
        ["shopping"] = "55555555-5555-5555-5555-555555555555",
        ["fun"] = "66666666-6666-6666-6666-666666666666",
        ["bills"] = "77777777-7777-7777-7777-777777777777",
        ["travel"] = "88888888-8888-8888-8888-888888888888"
    };
    private static readonly IReadOnlyDictionary<string, string> Income = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["salary"] = "99999999-9999-9999-9999-999999999991",
        ["bonus"] = "99999999-9999-9999-9999-999999999992",
        ["gift"] = "99999999-9999-9999-9999-999999999993",
        ["cashback"] = "99999999-9999-9999-9999-999999999994",
        ["sale"] = "99999999-9999-9999-9999-999999999995",
        ["interest"] = "99999999-9999-9999-9999-999999999996",
        ["refund"] = "99999999-9999-9999-9999-999999999997",
        ["other"] = "99999999-9999-9999-9999-999999999998"
    };
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
        if (!await TimeZone(link, token))
        {
            return true;
        }
        if (!await Exists(link, "category", token) || !await Exists(link, "transaction_entry", token) || !await Exists(link, "account_transfer", token))
        {
            return true;
        }
        if (!await Cascade(link, token))
        {
            return true;
        }
        if (!await Transfer(link, token))
        {
            return true;
        }
        return !await Catalog(link, ExpenseKind, Expense, token) || !await Catalog(link, IncomeKind, Income, token);
    }
    private static async ValueTask<bool> Exists(NpgsqlConnection link, string name, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select exists (select 1 from information_schema.tables where table_schema = 'finance' and table_name = @table_name)", link);
        note.Parameters.AddWithValue("table_name", name);
        object? data = await note.ExecuteScalarAsync(token);
        return data is true;
    }
    private static async ValueTask<bool> TimeZone(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new("""
                                            select exists
                                            (
                                                select 1
                                                from information_schema.columns
                                                where table_schema = 'finance'
                                                  and table_name = 'user_account'
                                                  and column_name = 'time_zone'
                                                  and is_nullable = 'NO'
                                            )
                                            """, link);
        object? data = await note.ExecuteScalarAsync(token);
        if (data is not true)
        {
            return false;
        }
        await using NpgsqlCommand item = new("select not exists (select 1 from finance.user_account where time_zone is null or btrim(time_zone) = '')", link);
        object? current = await item.ExecuteScalarAsync(token);
        return current is true;
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
        return data is true;
    }
    private static async ValueTask<bool> Transfer(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new("""
                                            select
                                                exists
                                                (
                                                    select 1
                                                    from pg_constraint item
                                                    join pg_class rel on rel.oid = item.conrelid
                                                    join pg_namespace space on space.oid = rel.relnamespace
                                                    where space.nspname = 'finance'
                                                      and rel.relname = 'account_transfer'
                                                      and item.conname = 'ck_account_transfer_distinct_accounts'
                                                      and pg_get_constraintdef(item.oid) = 'CHECK ((source_account_id <> target_account_id))'
                                                )
                                                and exists
                                                (
                                                    select 1
                                                    from pg_constraint item
                                                    join pg_class rel on rel.oid = item.conrelid
                                                    join pg_namespace space on space.oid = rel.relnamespace
                                                    where space.nspname = 'finance'
                                                      and rel.relname = 'account_transfer'
                                                      and item.conname = 'ck_account_transfer_positive_amount'
                                                      and pg_get_constraintdef(item.oid) = 'CHECK ((amount > (0)::numeric))'
                                                )
                                                and exists (select 1 from pg_indexes where schemaname = 'finance' and tablename = 'account_transfer' and indexname = 'idx_account_transfer_user_occurred')
                                                and exists (select 1 from pg_indexes where schemaname = 'finance' and tablename = 'account_transfer' and indexname = 'idx_account_transfer_source')
                                                and exists (select 1 from pg_indexes where schemaname = 'finance' and tablename = 'account_transfer' and indexname = 'idx_account_transfer_target')
                                                and exists
                                                (
                                                    select 1
                                                    from pg_proc item
                                                    join pg_namespace space on space.oid = item.pronamespace
                                                    where space.nspname = 'finance'
                                                      and item.proname = 'fn_account_transfer_integrity'
                                                      and position('where item.id = new.source_account_id and item.user_id = new.user_id' in pg_get_functiondef(item.oid)) > 0
                                                      and position('where item.id = new.target_account_id and item.user_id = new.user_id' in pg_get_functiondef(item.oid)) > 0
                                                      and position('new.currency_code <> source_currency' in pg_get_functiondef(item.oid)) > 0
                                                )
                                                and exists
                                                (
                                                    select 1
                                                    from pg_trigger item
                                                    join pg_class rel on rel.oid = item.tgrelid
                                                    join pg_namespace space on space.oid = rel.relnamespace
                                                    where space.nspname = 'finance'
                                                      and rel.relname = 'account_transfer'
                                                      and item.tgname = 'trg_account_transfer_integrity'
                                                      and not item.tgisinternal
                                                      and pg_get_triggerdef(item.oid, true) = 'CREATE TRIGGER trg_account_transfer_integrity BEFORE INSERT OR UPDATE ON finance.account_transfer FOR EACH ROW EXECUTE FUNCTION finance.fn_account_transfer_integrity()'
                                                )
                                            """, link);
        object? data = await note.ExecuteScalarAsync(token);
        return data is true;
    }
    private static async ValueTask<bool> Catalog(NpgsqlConnection link, string kind, IReadOnlyDictionary<string, string> expected, CancellationToken token)
    {
        IReadOnlyDictionary<string, string> actual = await Seed(link, kind, token);
        return expected.All(item => actual.TryGetValue(item.Key, out string? id) && string.Equals(id, item.Value, StringComparison.Ordinal));
    }
    private static async ValueTask<IReadOnlyDictionary<string, string>> Seed(NpgsqlConnection link, string kind, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select code, id::text from finance.category where scope = 'system' and kind = @kind and code is not null", link);
        note.Parameters.AddWithValue("kind", kind);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        Dictionary<string, string> item = new(StringComparer.Ordinal);
        while (await row.ReadAsync(token))
        {
            item[row.GetString(0)] = row.GetString(1);
        }
        return item;
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
                gate.Parameters.AddWithValue("key", Key);
                object? data = await gate.ExecuteScalarAsync(item.Token);
                if (data is true)
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
        note.Parameters.AddWithValue("key", Key);
        _ = await note.ExecuteScalarAsync(token);
    }
}
