#pragma warning disable S2325
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceSql
{
    private const string CreatedUtc = "created_utc";
    private const string UpdatedUtc = "updated_utc";
    private const string UserId = "user_id";
    private readonly WorkspaceBody body;

    internal WorkspaceSql(WorkspaceBody body) => this.body = body ?? throw new ArgumentNullException(nameof(body));

    internal async ValueTask<WorkspaceItem?> Read(NpgsqlConnection link, NpgsqlTransaction lane, string key, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id, state_code, state_data, revision from finance.workspace where conversation_key = @conversation_key and user_id = @user_id for update", link, lane);
        note.Parameters.AddWithValue("conversation_key", key);
        note.Parameters.AddWithValue(UserId, userId);
        return await Item(note, false, token);
    }

    internal async ValueTask<WorkspaceItem?> Add(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceFrame frame, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.workspace(id, user_id, conversation_key, state_code, state_data, revision, entry_payload, last_payload, created_utc, opened_utc, updated_utc) values (@id, @user_id, @conversation_key, @state_code, @state_data, @revision, @entry_payload, @last_payload, @created_utc, @opened_utc, @updated_utc) on conflict (user_id, conversation_key) do nothing returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(UserId, frame.UserValue);
        note.Parameters.AddWithValue("conversation_key", frame.Room);
        note.Parameters.AddWithValue("state_code", frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, frame.Body);
        note.Parameters.AddWithValue("revision", 1L);
        note.Parameters.AddWithValue("entry_payload", frame.Entry);
        note.Parameters.AddWithValue("last_payload", frame.Last);
        note.Parameters.AddWithValue(CreatedUtc, frame.When);
        note.Parameters.AddWithValue("opened_utc", frame.When);
        note.Parameters.AddWithValue(UpdatedUtc, frame.When);
        return await Item(note, true, token);
    }

    internal async ValueTask<WorkspaceItem?> Write(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceMark mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new("update finance.workspace set user_id = @user_id, state_code = @state_code, state_data = @state_data, last_payload = @last_payload, revision = revision + 1, opened_utc = @opened_utc, updated_utc = @updated_utc where id = @id and revision = @revision and user_id = @user_id returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", mark.IdValue);
        note.Parameters.AddWithValue("revision", mark.Revision);
        note.Parameters.AddWithValue(UserId, mark.Frame.UserValue);
        note.Parameters.AddWithValue("state_code", mark.Frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, mark.Frame.Body);
        note.Parameters.AddWithValue("last_payload", mark.Frame.Last);
        note.Parameters.AddWithValue("opened_utc", mark.Frame.When);
        note.Parameters.AddWithValue(UpdatedUtc, mark.Frame.When);
        return await Item(note, false, token);
    }

    internal async ValueTask<bool> Account(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, AccountDraft draft, DateTimeOffset when, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.account(id, user_id, name, currency_code, opening_amount, current_amount, created_utc, updated_utc) values (@id, @user_id, @name, @currency_code, @opening_amount, @current_amount, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(UserId, userId);
        note.Parameters.AddWithValue("name", draft.Title);
        note.Parameters.AddWithValue("currency_code", draft.Unit);
        note.Parameters.AddWithValue("opening_amount", draft.Total);
        note.Parameters.AddWithValue("current_amount", draft.Total);
        note.Parameters.AddWithValue(CreatedUtc, when);
        note.Parameters.AddWithValue(UpdatedUtc, when);
        return (await Id(note, token)).HasValue;
    }

    internal async ValueTask<IReadOnlyList<AccountData>> Accounts(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id::text, name, currency_code, current_amount from finance.account where user_id = @user_id order by created_utc, name", link, lane);
        note.Parameters.AddWithValue(UserId, userId);
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
        note.Parameters.AddWithValue(UserId, userId);
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
        note.Parameters.AddWithValue(UserId, userId);
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
        note.Parameters.AddWithValue(UserId, userId);
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

    internal async ValueTask<PickData> Category(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string value, string kind, DateTimeOffset when, CancellationToken token)
    {
        string text = value.Trim();
        await using (NpgsqlCommand find = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and (scope = 'system' or user_id = @user_id) order by case scope when 'system' then 0 else 1 end limit 1", link, lane))
        {
            find.Parameters.AddWithValue("kind", kind);
            find.Parameters.AddWithValue("name", text);
            find.Parameters.AddWithValue(UserId, userId);
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
            add.Parameters.AddWithValue(UserId, userId);
            add.Parameters.Add("code", NpgsqlDbType.Text).Value = DBNull.Value;
            add.Parameters.AddWithValue("name", text);
            add.Parameters.AddWithValue(CreatedUtc, when);
            add.Parameters.AddWithValue(UpdatedUtc, when);
            await using NpgsqlDataReader row = await add.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), row.GetString(2));
            }
        }
        await using NpgsqlCommand note = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and user_id = @user_id limit 1", link, lane);
        note.Parameters.AddWithValue("kind", kind);
        note.Parameters.AddWithValue("name", text);
        note.Parameters.AddWithValue(UserId, userId);
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
        item.Parameters.AddWithValue(UserId, userId);
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
        note.Parameters.AddWithValue(UserId, userId);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            return false;
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {body.Reverse(kind)} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", amount);
        data.Parameters.AddWithValue(UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(UserId, userId);
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
        item.Parameters.AddWithValue(UpdatedUtc, when);
        item.Parameters.AddWithValue("id", itemId);
        item.Parameters.AddWithValue(UserId, userId);
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
            item.Parameters.AddWithValue(UserId, userId);
            item.Parameters.AddWithValue("account_id", accountId);
            item.Parameters.AddWithValue("category_id", categoryId);
            item.Parameters.AddWithValue("kind", kind);
            item.Parameters.AddWithValue("amount", note.Total);
            item.Parameters.AddWithValue("occurred_utc", when);
            item.Parameters.AddWithValue(CreatedUtc, when);
            item.Parameters.AddWithValue(UpdatedUtc, when);
            if (await item.ExecuteNonQueryAsync(token) != 1)
            {
                throw new InvalidOperationException("Transaction insert failed");
            }
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {sign} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", note.Total);
        data.Parameters.AddWithValue(UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(UserId, userId);
        if (await data.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Account balance update failed");
        }
    }

    private int Page(int page, int shift)
    {
        int item = page + shift;
        return item < 0 ? 0 : item;
    }

    private Guid Parse(string value, string name) => Guid.TryParse(value, out Guid item) ? item : throw new ArgumentException("Workspace identity value is invalid", name);

    private async ValueTask<Guid?> Id(NpgsqlCommand note, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? row.GetGuid(0) : null;
    }

    private async ValueTask<WorkspaceItem?> Item(NpgsqlCommand note, bool isNew, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? new WorkspaceItem(row.GetGuid(0), new WorkspaceSnapshot(row.GetString(1), row.GetString(2), row.GetInt64(3), isNew)) : null;
    }
}
#pragma warning restore S2325
