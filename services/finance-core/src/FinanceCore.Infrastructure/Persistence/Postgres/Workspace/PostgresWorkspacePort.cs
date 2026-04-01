using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Workspace.Ports;
using FinanceCore.Domain.Workspace.Models;
using FinanceCore.Domain.Workspace.Policies;
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class PostgresWorkspacePort : IWorkspacePort, IWorkspaceInputPort
{
    private const int RetryCount = 8;
    private const string ViewContract = "workspace.view.requested";
    private const string ViewSource = "finance-core";
    private const string CreatedUtc = "created_utc";
    private const string UpdatedUtc = "updated_utc";
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource source;
    private readonly IWorkspaceActions policy;
    private readonly WorkspaceBody body;
    private readonly WorkspaceInput input;
    private readonly WorkspaceSql sql;

    internal PostgresWorkspacePort(NpgsqlDataSource source, IWorkspaceActions policy)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        body = new WorkspaceBody();
        var amount = new WorkspaceAmount();
        var draft = new WorkspaceDraft(body, amount);
        var recent = new WorkspaceRecent(body);
        var summary = new WorkspaceSummary(body);
        var breakdown = new WorkspaceBreakdown(body);
        input = new WorkspaceInput(body, draft, recent, summary, breakdown);
        sql = new WorkspaceSql(body);
    }

    public async ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTimeOffset when = body.Utc(message.Payload.OccurredUtc, nameof(message));
        string raw = JsonSerializer.Serialize(message, json);
        await using NpgsqlConnection link = await source.OpenConnectionAsync(token);
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync(token);
        bool fresh = await Inbox(link, lane, message, raw, token);
        if (!fresh)
        {
            await lane.CommitAsync(token);
            return;
        }
        (Guid userId, bool isNewUser) = await User(link, lane, message.Payload.Identity, message.Payload.Profile, when, token);
        WorkspaceWrite state = await Start(link, lane, userId, message.Payload, when, token);
        await Outbox(link, lane, message, state.State, new WorkspaceViewNote(message.Payload.Identity, message.Payload.Profile, isNewUser, state.IsNew, when), token);
        await Processed(link, lane, message, token);
        await lane.CommitAsync(token);
    }

    public async ValueTask Save(MessageEnvelope<WorkspaceInputRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTimeOffset when = body.Utc(message.Payload.OccurredUtc, nameof(message));
        string raw = JsonSerializer.Serialize(message, json);
        await using NpgsqlConnection link = await source.OpenConnectionAsync(token);
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync(token);
        bool fresh = await Inbox(link, lane, message, raw, token);
        if (!fresh)
        {
            await lane.CommitAsync(token);
            return;
        }
        (Guid userId, bool isNewUser) = await User(link, lane, message.Payload.Identity, message.Payload.Profile, when, token);
        WorkspaceWrite state = await Input(link, lane, userId, message.Payload, when, token);
        await Outbox(link, lane, message, state.State, new WorkspaceViewNote(message.Payload.Identity, message.Payload.Profile, isNewUser, state.IsNew, when), token);
        await Processed(link, lane, message, token);
        await lane.CommitAsync(token);
    }

    private async ValueTask<WorkspaceWrite> Start(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            WorkspaceItem? current = await sql.Read(link, lane, command.Identity.ConversationKey, userId, token);
            IReadOnlyList<AccountData> list = await sql.Accounts(link, lane, userId, token);
            WorkspaceData state = body.Home(list, string.Empty);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, WorkspaceBody.HomeState, body.Json(state), command.Payload, command.Payload, when);
            WorkspaceItem? next = current is null ? await sql.Add(link, lane, frame, token) : await sql.Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                return new WorkspaceWrite(next, current is null);
            }
        }
        throw new InvalidOperationException($"Workspace save exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }

    private async ValueTask<WorkspaceWrite> Input(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceInputRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            string mark = $"workspace_input_{item}";
            await Save(link, lane, mark, token);
            WorkspaceItem? current = await sql.Read(link, lane, command.Identity.ConversationKey, userId, token);
            IReadOnlyList<AccountData> list = await sql.Accounts(link, lane, userId, token);
            WorkspaceData state = current is null ? body.Home(list, string.Empty) : body.Sync(body.Data(current.Snapshot.Data), list);
            string code = current?.Snapshot.State ?? WorkspaceBody.HomeState;
            WorkspaceMove move = await Flow(link, lane, userId, input.Move(code, state, command, when), when, token);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, move.Code, body.Json(move.Body), string.Empty, command.Value, when);
            WorkspaceItem? next = current is null ? await sql.Add(link, lane, frame, token) : await sql.Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                await Release(link, lane, mark, token);
                return new WorkspaceWrite(next, current is null);
            }
            await Revert(link, lane, mark, token);
        }
        throw new InvalidOperationException($"Workspace input exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }

    private async ValueTask<WorkspaceMove> Flow(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        move = await Pick(link, lane, userId, move, when, token);
        move = await Store(link, lane, userId, move, when, token);
        move = await Fill(link, lane, userId, move, token);
        move = await Track(link, lane, userId, move, when, token);
        move = await Correct(link, lane, userId, move, when, token);
        return await Finish(link, lane, userId, move, token);
    }

    private async ValueTask<WorkspaceMove> Pick(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token) => string.IsNullOrWhiteSpace(move.CategoryEntry) ? move : await CategoryPick(link, lane, userId, move, when, token);

    private async ValueTask<WorkspaceMove> Store(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.AccountValue is null)
        {
            return move;
        }
        bool fresh = await sql.Account(link, lane, userId, move.AccountValue, when, token);
        return fresh ? move : new WorkspaceMove(WorkspaceBody.NameState, body.Account(move.Body, new FinancialData(move.AccountValue.Title, move.AccountValue.Unit, move.AccountValue.Total), new StatusData("Account name already exists", string.Empty)), null, string.Empty, null);
    }

    private async ValueTask<WorkspaceMove> Fill(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, CancellationToken token)
    {
        if (move.Code == WorkspaceBody.RecentListState && move.Body.Recent.Items.Count == 0)
        {
            RecentData page = await sql.Recent(link, lane, userId, move.Body.Recent.Page, token);
            return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, page, new ChoicesData(), move.Body.Status), null, string.Empty, null);
        }
        if (move.Code == WorkspaceBody.SummaryState && move.Body.Summary.Year > 0 && move.Body.Summary.Month > 0 && move.Body.Summary.Currencies.Count == 0)
        {
            SummaryData item = await sql.Summary(link, lane, userId, move.Body.Summary.Year, move.Body.Summary.Month, token);
            return new WorkspaceMove(WorkspaceBody.SummaryState, body.Summary(move.Body, item, move.Body.Status), null, string.Empty, null);
        }
        if (move.Code == WorkspaceBody.BreakdownState && move.Body.Breakdown.Year > 0 && move.Body.Breakdown.Month > 0 && move.Body.Breakdown.Currencies.Count == 0)
        {
            BreakdownData item = await sql.Breakdown(link, lane, userId, move.Body.Breakdown.Year, move.Body.Breakdown.Month, token);
            return new WorkspaceMove(WorkspaceBody.BreakdownState, body.Breakdown(move.Body, item, move.Body.Status), null, string.Empty, null);
        }
        if (!body.TransactionCategoryState(move.Code) || move.Body.Choices.Categories.Count > 0)
        {
            return move;
        }
        string kind = move.Code == WorkspaceBody.RecentCategoryState ? move.Body.Recent.Selected.Kind : body.Kind(move.Code);
        IReadOnlyList<OptionData> list = await sql.Categories(link, lane, userId, kind, token);
        return new WorkspaceMove(move.Code, body.Model(move.Body, choices: new ChoicesData([], list), status: move.Body.Status), null, string.Empty, null);
    }

    private async ValueTask<WorkspaceMove> Track(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.RecordValue is null)
        {
            return move;
        }
        await sql.Transaction(link, lane, userId, move.RecordValue, when, token);
        return new WorkspaceMove(WorkspaceBody.HomeState, body.Home(await sql.Accounts(link, lane, userId, token), move.RecordValue.TransactionKind == WorkspaceBody.IncomeKind ? "Income was recorded" : "Expense was recorded"), null, string.Empty, null);
    }

    private async ValueTask<WorkspaceMove> Correct(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.CorrectValue is null)
        {
            return move;
        }
        if (move.CorrectValue.Mode == WorkspaceBody.DeleteMode)
        {
            bool ok = await sql.Delete(link, lane, userId, move.CorrectValue.TransactionId, move.CorrectValue.TransactionKind, when, token);
            RecentData page = await sql.Current(link, lane, userId, move.Body.Recent.Page, token);
            return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, page, new ChoicesData(), new StatusData(string.Empty, ok ? "Transaction was deleted" : WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null);
        }
        bool fresh = await sql.Recategorize(link, lane, userId, move.CorrectValue, when, token);
        RecentData current = await sql.Recent(link, lane, userId, move.Body.Recent.Page, token);
        return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, current, new ChoicesData(), new StatusData(string.Empty, fresh ? "Category was updated" : WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null);
    }

    private async ValueTask<WorkspaceMove> Finish(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, CancellationToken token)
    {
        if (move.Code == WorkspaceBody.HomeState)
        {
            return new WorkspaceMove(WorkspaceBody.HomeState, body.Home(await sql.Accounts(link, lane, userId, token), move.Body.Status.Notice, move.Body.Status.Error), null, string.Empty, null);
        }
        if (move.Code == WorkspaceBody.RecentDetailState && !string.IsNullOrWhiteSpace(move.Body.Recent.Selected.Id))
        {
            RecentItemData? item = await sql.Recent(link, lane, userId, move.Body.Recent.Selected.Id, token);
            if (item is null)
            {
                RecentData page = await sql.Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, page, new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null);
            }
            return new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Recent(move.Body, new RecentData(move.Body.Recent.Page, move.Body.Recent.HasPrevious, move.Body.Recent.HasNext, move.Body.Recent.Items, item), move.Body.Choices, move.Body.Status), null, string.Empty, null);
        }
        return move;
    }

    private async ValueTask<WorkspaceMove> CategoryPick(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.Code == WorkspaceBody.RecentCategoryState && move.CorrectValue is not null)
        {
            if (string.IsNullOrWhiteSpace(move.Body.Recent.Selected.Id))
            {
                RecentData page = await sql.Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, page, new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null);
            }
            RecentItemData? current = await sql.Recent(link, lane, userId, move.Body.Recent.Selected.Id, token);
            if (current is null)
            {
                RecentData page = await sql.Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(move.Body, page, new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null);
            }
            PickData pick = await sql.Category(link, lane, userId, move.CategoryEntry, move.CorrectValue.TransactionKind, when, token);
            WorkspaceData state = body.Recent(move.Body, new RecentData(move.Body.Recent.Page, move.Body.Recent.HasPrevious, move.Body.Recent.HasNext, move.Body.Recent.Items, new RecentItemData(current.Slot, new RecentEntryData(current.Id, current.Kind, current.Account, pick, current.Amount, current.Currency, current.OccurredUtc))), move.Body.Choices, move.Body.Status);
            return new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, state, null, string.Empty, null);
        }
        bool income = body.Kind(move.Code) == WorkspaceBody.IncomeKind;
        PickData item = await sql.Category(link, lane, userId, move.CategoryEntry, body.Kind(income), when, token);
        WorkspaceData data = body.Transaction(move.Body, body.Pick(move.Body, income), item, body.Total(move.Body, income), income);
        return new WorkspaceMove(body.ConfirmCode(income), data, null, string.Empty, null);
    }

    private static async ValueTask Save(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }

    private static async ValueTask Revert(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"rollback to savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }

    private static async ValueTask Release(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"release savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }

    private static async ValueTask<bool> Inbox<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, string payload, CancellationToken token) where TMessage : class
    {
        await using NpgsqlCommand note = new("insert into finance.inbox_message(message_id, contract, source, correlation_id, causation_id, idempotency_key, payload, received_utc, processed_utc, attempt) values (@message_id, @contract, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @received_utc, @processed_utc, @attempt) on conflict do nothing", link, lane);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("source", message.Source);
        note.Parameters.AddWithValue("correlation_id", message.Context.CorrelationId);
        note.Parameters.AddWithValue("causation_id", message.Context.CausationId);
        note.Parameters.AddWithValue("idempotency_key", message.Context.IdempotencyKey);
        note.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
        note.Parameters.AddWithValue("received_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("processed_utc", DBNull.Value);
        note.Parameters.AddWithValue("attempt", 1);
        return await note.ExecuteNonQueryAsync(token) == 1;
    }

    private static async ValueTask<(Guid UserId, bool IsNewUser)> User(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceIdentity identity, WorkspaceProfile profile, DateTimeOffset when, CancellationToken token)
    {
        await using NpgsqlCommand add = new("insert into finance.user_account(id, actor_key, name, locale, created_utc, updated_utc) values (@id, @actor_key, @name, @locale, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        add.Parameters.AddWithValue("id", Guid.CreateVersion7());
        add.Parameters.AddWithValue("actor_key", identity.ActorKey);
        add.Parameters.AddWithValue("name", profile.Name);
        add.Parameters.AddWithValue("locale", profile.Locale);
        add.Parameters.AddWithValue(CreatedUtc, when);
        add.Parameters.AddWithValue(UpdatedUtc, when);
        Guid? userId = await Id(add, token);
        if (userId.HasValue)
        {
            return (userId.Value, true);
        }
        await using NpgsqlCommand note = new("update finance.user_account set name = @name, locale = @locale, updated_utc = @updated_utc where actor_key = @actor_key returning id", link, lane);
        note.Parameters.AddWithValue("actor_key", identity.ActorKey);
        note.Parameters.AddWithValue("name", profile.Name);
        note.Parameters.AddWithValue("locale", profile.Locale);
        note.Parameters.AddWithValue(UpdatedUtc, when);
        userId = await Id(note, token);
        if (userId.HasValue)
        {
            return (userId.Value, false);
        }
        throw new InvalidOperationException("User upsert failed");
    }

    private async ValueTask Outbox<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, WorkspaceItem item, WorkspaceViewNote note, CancellationToken token) where TMessage : class
    {
        var state = new WorkspaceState(item.Snapshot.State, item.Snapshot.Data, item.Snapshot.Revision);
        var view = new WorkspaceView(note.Identity, note.Profile, state, policy.Codes(state.Code, body.Context(body.Data(state.Data), note.When)), note.IsNewUser, note.IsNewWorkspace, note.When);
        var data = new WorkspaceViewRequestedCommand(view.Identity, view.Profile, new WorkspaceViewFrame(view.State.Code, view.State.Data, view.Actions), new WorkspaceViewFreshness(view.IsNewUser, view.IsNewWorkspace), view.OccurredUtc);
        var envelope = new MessageEnvelope<WorkspaceViewRequestedCommand>(Guid.CreateVersion7(), ViewContract, note.When, new MessageContext(message.Context.CorrelationId, message.MessageId.ToString(), $"{message.Context.IdempotencyKey}:workspace-view"), ViewSource, data);
        string raw = JsonSerializer.Serialize(envelope, json);
        await using NpgsqlCommand itemNote = new("insert into finance.outbox_message(message_id, contract, routing_key, source, correlation_id, causation_id, idempotency_key, payload, occurred_utc, created_utc, published_utc, attempt, error) values (@message_id, @contract, @routing_key, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @occurred_utc, @created_utc, @published_utc, @attempt, @error) on conflict do nothing", link, lane);
        itemNote.Parameters.AddWithValue("message_id", envelope.MessageId);
        itemNote.Parameters.AddWithValue("contract", ViewContract);
        itemNote.Parameters.AddWithValue("routing_key", ViewContract);
        itemNote.Parameters.AddWithValue("source", ViewSource);
        itemNote.Parameters.AddWithValue("correlation_id", envelope.Context.CorrelationId);
        itemNote.Parameters.AddWithValue("causation_id", envelope.Context.CausationId);
        itemNote.Parameters.AddWithValue("idempotency_key", envelope.Context.IdempotencyKey);
        itemNote.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, raw);
        itemNote.Parameters.AddWithValue("occurred_utc", view.OccurredUtc);
        itemNote.Parameters.AddWithValue(CreatedUtc, DateTimeOffset.UtcNow);
        itemNote.Parameters.AddWithValue("published_utc", DBNull.Value);
        itemNote.Parameters.AddWithValue("attempt", 0);
        itemNote.Parameters.AddWithValue("error", string.Empty);
        if (await itemNote.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Outbox insert failed");
        }
    }

    private static async ValueTask Processed<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        await using NpgsqlCommand note = new("update finance.inbox_message set processed_utc = @processed_utc where contract = @contract and message_id = @message_id", link, lane);
        note.Parameters.AddWithValue("processed_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Inbox processed update failed");
        }
    }

    private static async ValueTask<Guid?> Id(NpgsqlCommand note, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? row.GetGuid(0) : null;
    }
}
