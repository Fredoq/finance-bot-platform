using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceSql
{
    private readonly WorkspaceBody body;
    private readonly WorkspaceSqlMap map;

    internal WorkspaceSql(WorkspaceBody body)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        map = new WorkspaceSqlMap();
    }

    internal async ValueTask<WorkspaceItem?> Read(NpgsqlConnection link, NpgsqlTransaction lane, string key, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id, state_code, state_data, revision from finance.workspace where conversation_key = @conversation_key and user_id = @user_id for update", link, lane);
        note.Parameters.AddWithValue("conversation_key", key);
        note.Parameters.AddWithValue(map.UserId, userId);
        return await Item(note, false, token);
    }

    internal async ValueTask<WorkspaceItem?> Add(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceFrame frame, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.workspace(id, user_id, conversation_key, state_code, state_data, revision, entry_payload, last_payload, created_utc, opened_utc, updated_utc) values (@id, @user_id, @conversation_key, @state_code, @state_data, @revision, @entry_payload, @last_payload, @created_utc, @opened_utc, @updated_utc) on conflict (user_id, conversation_key) do nothing returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(map.UserId, frame.UserValue);
        note.Parameters.AddWithValue("conversation_key", frame.Room);
        note.Parameters.AddWithValue("state_code", frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, frame.Body);
        note.Parameters.AddWithValue("revision", 1L);
        note.Parameters.AddWithValue("entry_payload", frame.Entry);
        note.Parameters.AddWithValue("last_payload", frame.Last);
        note.Parameters.AddWithValue(map.CreatedUtc, frame.When);
        note.Parameters.AddWithValue("opened_utc", frame.When);
        note.Parameters.AddWithValue(map.UpdatedUtc, frame.When);
        return await Item(note, true, token);
    }

    internal async ValueTask<WorkspaceItem?> Write(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceMark mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new("update finance.workspace set user_id = @user_id, state_code = @state_code, state_data = @state_data, last_payload = @last_payload, revision = revision + 1, opened_utc = @opened_utc, updated_utc = @updated_utc where id = @id and revision = @revision and user_id = @user_id returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", mark.IdValue);
        note.Parameters.AddWithValue("revision", mark.Revision);
        note.Parameters.AddWithValue(map.UserId, mark.Frame.UserValue);
        note.Parameters.AddWithValue("state_code", mark.Frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, mark.Frame.Body);
        note.Parameters.AddWithValue("last_payload", mark.Frame.Last);
        note.Parameters.AddWithValue("opened_utc", mark.Frame.When);
        note.Parameters.AddWithValue(map.UpdatedUtc, mark.Frame.When);
        return await Item(note, false, token);
    }

    internal async ValueTask<bool> Account(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, AccountDraft draft, DateTimeOffset when, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.account(id, user_id, name, currency_code, opening_amount, current_amount, created_utc, updated_utc) values (@id, @user_id, @name, @currency_code, @opening_amount, @current_amount, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("name", draft.Title);
        note.Parameters.AddWithValue("currency_code", draft.Unit);
        note.Parameters.AddWithValue("opening_amount", draft.Total);
        note.Parameters.AddWithValue("current_amount", draft.Total);
        note.Parameters.AddWithValue(map.CreatedUtc, when);
        note.Parameters.AddWithValue(map.UpdatedUtc, when);
        return (await Id(note, token)).HasValue;
    }

    internal async ValueTask<IReadOnlyList<AccountData>> Accounts(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id::text, name, currency_code, current_amount from finance.account where user_id = @user_id order by created_utc, name", link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        List<AccountData> list = [];
        while (await row.ReadAsync(token))
        {
            list.Add(new AccountData(row.GetString(0), row.GetString(1), row.GetString(2), row.GetDecimal(3)));
        }
        return list;
    }

    internal async ValueTask<IReadOnlyList<OptionData>> Categories(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string kind, CancellationToken token)
    {
        List<OptionData> list = [];
        await using NpgsqlCommand note = new("""
                                             with recent(category_id, occurred_utc) as
                                             (
                                                 select category_id, max(occurred_utc) as occurred_utc
                                                 from finance.transaction_entry
                                                 where user_id = @user_id and kind = @kind
                                                 group by category_id
                                             ),
                                             custom(id, name, code, area, order_id, occurred_utc) as
                                             (
                                                 select c.id::text, c.name, '' as code, 1 as area, 999 as order_id, recent.occurred_utc
                                                 from finance.category c
                                                 join recent on recent.category_id = c.id
                                                 where c.user_id = @user_id and c.kind = @kind and c.scope = 'user'
                                                 order by recent.occurred_utc desc, c.name
                                                 limit 6
                                             )
                                             select id, name, code
                                             from
                                             (
                                                 select id::text, name, coalesce(code, '') as code, 0 as area,
                                                        case
                                                            when @kind = 'expense' then case code when 'food' then 1 when 'transport' then 2 when 'home' then 3 when 'health' then 4 when 'shopping' then 5 when 'fun' then 6 when 'bills' then 7 when 'travel' then 8 else 999 end
                                                            else case code when 'salary' then 1 when 'bonus' then 2 when 'gift' then 3 when 'cashback' then 4 when 'sale' then 5 when 'interest' then 6 when 'refund' then 7 when 'other' then 8 else 999 end
                                                        end as order_id,
                                                        null::timestamptz as occurred_utc
                                                 from finance.category
                                                 where kind = @kind and scope = 'system'
                                                 union all
                                                 select id, name, code, area, order_id, occurred_utc
                                                 from custom
                                             ) item
                                             order by area, order_id, occurred_utc desc nulls last, name
                                             """, link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("kind", kind);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        while (await row.ReadAsync(token))
        {
            list.Add(new OptionData(list.Count + 1, row.GetString(0), row.GetString(1), row.GetString(2)));
        }
        return list;
    }

    internal async ValueTask<RecentData> Recent(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int page, CancellationToken token)
    {
        int index = Page(page, 0);
        int offset = index * WorkspaceBody.RecentPageSize;
        List<RecentItemData> list = [];
        await using NpgsqlCommand note = new("""
                                             select item.id::text,
                                                    item.kind,
                                                    account.id::text,
                                                    account.name,
                                                    account.currency_code,
                                                    category.id::text,
                                                    category.name,
                                                    coalesce(category.code, ''),
                                                    item.amount,
                                                    item.occurred_utc
                                             from finance.transaction_entry item
                                             join finance.account account on account.id = item.account_id
                                             join finance.category category on category.id = item.category_id
                                             where item.user_id = @user_id
                                             order by item.occurred_utc desc, item.created_utc desc, item.id desc
                                             offset @offset
                                             limit @limit
                                             """, link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("offset", offset);
        note.Parameters.AddWithValue("limit", WorkspaceBody.RecentPageSize + 1);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        while (await row.ReadAsync(token))
        {
            list.Add(new RecentItemData(list.Count + 1, new RecentEntryData(row.GetString(0), row.GetString(1), new PickData(row.GetString(2), row.GetString(3), row.GetString(4)), new PickData(row.GetString(5), row.GetString(6), row.GetString(7)), row.GetDecimal(8), row.GetString(4), await row.GetFieldValueAsync<DateTimeOffset>(9, token))));
        }
        bool hasNext = list.Count > WorkspaceBody.RecentPageSize;
        RecentItemData[] items = hasNext ? [.. list.Take(WorkspaceBody.RecentPageSize)] : [.. list];
        for (int item = 0; item < items.Length; item += 1)
        {
            items[item] = new RecentItemData(item + 1, new RecentEntryData(items[item].Id, items[item].Kind, items[item].Account, items[item].Category, items[item].Amount, items[item].Currency, items[item].OccurredUtc));
        }
        return new RecentData(index, index > 0, hasNext, items, new RecentItemData());
    }

    internal async ValueTask<RecentItemData?> Recent(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string transactionId, CancellationToken token)
    {
        Guid itemId = Parse(transactionId, nameof(transactionId));
        await using NpgsqlCommand note = new("""
                                             select item.id::text,
                                                    item.kind,
                                                    account.id::text,
                                                    account.name,
                                                    account.currency_code,
                                                    category.id::text,
                                                    category.name,
                                                    coalesce(category.code, ''),
                                                    item.amount,
                                                    item.occurred_utc
                                             from finance.transaction_entry item
                                             join finance.account account on account.id = item.account_id
                                             join finance.category category on category.id = item.category_id
                                             where item.user_id = @user_id and item.id = @id
                                             limit 1
                                             """, link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("id", itemId);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        if (!await row.ReadAsync(token))
        {
            return null;
        }
        return new RecentItemData(0, new RecentEntryData(row.GetString(0), row.GetString(1), new PickData(row.GetString(2), row.GetString(3), row.GetString(4)), new PickData(row.GetString(5), row.GetString(6), row.GetString(7)), row.GetDecimal(8), row.GetString(4), await row.GetFieldValueAsync<DateTimeOffset>(9, token)));
    }

    internal async ValueTask<RecentData> Current(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int page, CancellationToken token)
    {
        RecentData item = await Recent(link, lane, userId, Page(page, 0), token);
        while (item.Items.Count == 0 && item.Page > 0)
        {
            item = await Recent(link, lane, userId, Page(item.Page, -1), token);
        }
        return item;
    }

    internal async ValueTask<SummaryData> Summary(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int year, int month, CancellationToken token)
    {
        DateTimeOffset start = new(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMonths(1);
        await using NpgsqlCommand note = new("""
                                             select account.currency_code,
                                                    account.id::text,
                                                    account.name,
                                                    coalesce(sum(case when item.kind = 'income' then item.amount else 0 end), 0),
                                                    coalesce(sum(case when item.kind = 'expense' then item.amount else 0 end), 0)
                                             from finance.transaction_entry item
                                             join finance.account account on account.id = item.account_id
                                             where item.user_id = @user_id and item.occurred_utc >= @start_utc and item.occurred_utc < @end_utc
                                             group by account.currency_code, account.id, account.name
                                             order by account.currency_code, account.name
                                             """, link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("start_utc", start);
        note.Parameters.AddWithValue("end_utc", end);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        List<SummaryCurrencyData> currencies = [];
        List<SummaryAccountData> accounts = [];
        string current = string.Empty;
        decimal income = 0m;
        decimal expense = 0m;
        while (await row.ReadAsync(token))
        {
            string currency = row.GetString(0);
            if (!string.Equals(current, currency, StringComparison.Ordinal) && accounts.Count > 0)
            {
                currencies.Add(new SummaryCurrencyData(current, income, expense, income - expense, accounts));
                accounts = [];
                income = 0m;
                expense = 0m;
            }
            current = currency;
            decimal accountIncome = row.GetDecimal(3);
            decimal accountExpense = row.GetDecimal(4);
            income += accountIncome;
            expense += accountExpense;
            accounts.Add(new SummaryAccountData(row.GetString(1), row.GetString(2), accountIncome, accountExpense, accountIncome - accountExpense));
        }
        if (accounts.Count > 0)
        {
            currencies.Add(new SummaryCurrencyData(current, income, expense, income - expense, accounts));
        }
        return new SummaryData(year, month, currencies);
    }

    internal async ValueTask<BreakdownData> Breakdown(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int year, int month, CancellationToken token)
    {
        DateTimeOffset start = new(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMonths(1);
        await using NpgsqlCommand note = new("""
                                             with category_total as
                                             (
                                                 select account.currency_code,
                                                        category.name,
                                                        coalesce(category.code, '') as code,
                                                        sum(item.amount) as amount
                                                 from finance.transaction_entry item
                                                 join finance.account account on account.id = item.account_id
                                                 join finance.category category on category.id = item.category_id
                                                 where item.user_id = @user_id and item.kind = 'expense' and item.occurred_utc >= @start_utc and item.occurred_utc < @end_utc
                                                 group by account.currency_code, category.name, coalesce(category.code, '')
                                             )
                                             select currency_code,
                                                    name,
                                                    code,
                                                    amount,
                                                    sum(amount) over(partition by currency_code) as total
                                             from category_total
                                             order by currency_code, amount desc, name
                                             """, link, lane);
        note.Parameters.AddWithValue(map.UserId, userId);
        note.Parameters.AddWithValue("start_utc", start);
        note.Parameters.AddWithValue("end_utc", end);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        List<BreakdownCurrencyData> currencies = [];
        List<BreakdownCategoryData> categories = [];
        string current = string.Empty;
        decimal total = 0m;
        while (await row.ReadAsync(token))
        {
            string currency = row.GetString(0);
            if (!string.Equals(current, currency, StringComparison.Ordinal) && categories.Count > 0)
            {
                currencies.Add(new BreakdownCurrencyData(current, total, categories));
                categories = [];
            }
            current = currency;
            decimal amount = row.GetDecimal(3);
            total = row.GetDecimal(4);
            decimal share = total == 0m ? 0m : amount / total;
            categories.Add(new BreakdownCategoryData(row.GetString(1), row.GetString(2), amount, share));
        }
        if (categories.Count > 0)
        {
            currencies.Add(new BreakdownCurrencyData(current, total, categories));
        }
        return new BreakdownData(year, month, currencies);
    }

    internal async ValueTask<PickData> Category(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string value, string kind, DateTimeOffset when, CancellationToken token)
    {
        string text = value.Trim();
        await using (NpgsqlCommand find = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and (scope = 'system' or user_id = @user_id) order by case scope when 'system' then 0 else 1 end limit 1", link, lane))
        {
            find.Parameters.AddWithValue("kind", kind);
            find.Parameters.AddWithValue("name", text);
            find.Parameters.AddWithValue(map.UserId, userId);
            await using NpgsqlDataReader row = await find.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), row.GetString(2));
            }
        }
        await using (NpgsqlCommand add = new("insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc) values (@id, @kind, @scope, @user_id, @code, @name, @created_utc, @updated_utc) on conflict do nothing returning id::text, name, coalesce(code, '')", link, lane))
        {
            add.Parameters.AddWithValue("id", Guid.CreateVersion7());
            add.Parameters.AddWithValue("kind", kind);
            add.Parameters.AddWithValue("scope", "user");
            add.Parameters.AddWithValue(map.UserId, userId);
            add.Parameters.Add("code", NpgsqlDbType.Text).Value = DBNull.Value;
            add.Parameters.AddWithValue("name", text);
            add.Parameters.AddWithValue(map.CreatedUtc, when);
            add.Parameters.AddWithValue(map.UpdatedUtc, when);
            await using NpgsqlDataReader row = await add.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), row.GetString(2));
            }
        }
        await using NpgsqlCommand note = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and user_id = @user_id limit 1", link, lane);
        note.Parameters.AddWithValue("kind", kind);
        note.Parameters.AddWithValue("name", text);
        note.Parameters.AddWithValue(map.UserId, userId);
        await using NpgsqlDataReader data = await note.ExecuteReaderAsync(token);
        if (await data.ReadAsync(token))
        {
            return new PickData(data.GetString(0), data.GetString(1), data.GetString(2));
        }
        throw new InvalidOperationException("Category upsert failed");
    }

    internal async ValueTask<bool> Delete(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string transactionId, string kind, DateTimeOffset when, CancellationToken token)
    {
        Guid itemId = Parse(transactionId, nameof(transactionId));
        await using NpgsqlCommand item = new("select account_id, amount from finance.transaction_entry where id = @id and user_id = @user_id for update", link, lane);
        item.Parameters.AddWithValue("id", itemId);
        item.Parameters.AddWithValue(map.UserId, userId);
        Guid accountId;
        decimal amount;
        await using (NpgsqlDataReader row = await item.ExecuteReaderAsync(token))
        {
            if (!await row.ReadAsync(token))
            {
                return false;
            }
            accountId = row.GetGuid(0);
            amount = row.GetDecimal(1);
        }
        await using NpgsqlCommand note = new("delete from finance.transaction_entry where id = @id and user_id = @user_id", link, lane);
        note.Parameters.AddWithValue("id", itemId);
        note.Parameters.AddWithValue(map.UserId, userId);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            return false;
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {body.Reverse(kind)} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", amount);
        data.Parameters.AddWithValue(map.UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(map.UserId, userId);
        if (await data.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Account balance update failed");
        }
        return true;
    }

    internal async ValueTask<bool> Recategorize(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CorrectionNote note, DateTimeOffset when, CancellationToken token)
    {
        Guid itemId = Parse(note.TransactionId, nameof(note.TransactionId));
        string categoryId = note.CategoryId;
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return false;
        }
        await using NpgsqlCommand item = new("update finance.transaction_entry set category_id = @category_id, updated_utc = @updated_utc where id = @id and user_id = @user_id", link, lane);
        item.Parameters.AddWithValue("category_id", Parse(categoryId, nameof(note.CategoryId)));
        item.Parameters.AddWithValue(map.UpdatedUtc, when);
        item.Parameters.AddWithValue("id", itemId);
        item.Parameters.AddWithValue(map.UserId, userId);
        return await item.ExecuteNonQueryAsync(token) == 1;
    }

    internal async ValueTask Transaction(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, TransactionNote note, DateTimeOffset when, CancellationToken token)
    {
        Guid accountId = Parse(note.AccountId, nameof(note.AccountId));
        Guid categoryId = Parse(note.CategoryId, nameof(note.CategoryId));
        string kind = body.Supported(note.TransactionKind);
        string sign = body.Change(kind);
        await using (NpgsqlCommand item = new("insert into finance.transaction_entry(id, user_id, account_id, category_id, kind, amount, occurred_utc, created_utc, updated_utc) values (@id, @user_id, @account_id, @category_id, @kind, @amount, @occurred_utc, @created_utc, @updated_utc)", link, lane))
        {
            item.Parameters.AddWithValue("id", Guid.CreateVersion7());
            item.Parameters.AddWithValue(map.UserId, userId);
            item.Parameters.AddWithValue("account_id", accountId);
            item.Parameters.AddWithValue("category_id", categoryId);
            item.Parameters.AddWithValue("kind", kind);
            item.Parameters.AddWithValue("amount", note.Total);
            item.Parameters.AddWithValue("occurred_utc", when);
            item.Parameters.AddWithValue(map.CreatedUtc, when);
            item.Parameters.AddWithValue(map.UpdatedUtc, when);
            if (await item.ExecuteNonQueryAsync(token) != 1)
            {
                throw new InvalidOperationException("Transaction insert failed");
            }
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {sign} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", note.Total);
        data.Parameters.AddWithValue(map.UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(map.UserId, userId);
        if (await data.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Account balance update failed");
        }
    }

    private int Page(int page, int shift) => map.Page(page, shift);

    private Guid Parse(string value, string name) => map.Parse(value, name);

    private async ValueTask<Guid?> Id(NpgsqlCommand note, CancellationToken token) => await map.Id(note, token);

    private async ValueTask<WorkspaceItem?> Item(NpgsqlCommand note, bool isNew, CancellationToken token) => await map.Item(note, isNew, token);

    private sealed class WorkspaceSqlMap
    {
        internal WorkspaceSqlMap()
        {
            CreatedUtc = "created_utc";
            UpdatedUtc = "updated_utc";
            UserId = "user_id";
            Zero = 0;
            IdentityError = "Workspace identity value is invalid";
        }

        internal string CreatedUtc { get; }

        internal string IdentityError { get; }

        internal string UpdatedUtc { get; }

        internal string UserId { get; }

        internal int Zero { get; }

        internal async ValueTask<Guid?> Id(NpgsqlCommand note, CancellationToken token)
        {
            await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
            return await row.ReadAsync(token) ? row.GetGuid(Zero) : null;
        }

        internal async ValueTask<WorkspaceItem?> Item(NpgsqlCommand note, bool isNew, CancellationToken token)
        {
            await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
            return await row.ReadAsync(token) ? new WorkspaceItem(row.GetGuid(Zero), new WorkspaceSnapshot(row.GetString(Zero + 1), row.GetString(Zero + 2), row.GetInt64(Zero + 3), isNew)) : null;
        }

        internal Guid Parse(string value, string name) => Guid.TryParse(value, out Guid item) ? item : throw new ArgumentException(IdentityError, name);

        internal int Page(int page, int shift)
        {
            int item = page + shift;
            return item < Zero ? Zero : item;
        }
    }
}
