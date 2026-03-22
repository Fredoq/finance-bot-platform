using System.Globalization;
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
    private const string HomeState = "home";
    private const string NameState = "account.name";
    private const string CurrencyState = "account.currency";
    private const string BalanceState = "account.balance";
    private const string ConfirmState = "account.confirm";
    private const string ViewContract = "workspace.view.requested";
    private const string ViewSource = "finance-core";
    private const string CreatedUtc = "created_utc";
    private const string UpdatedUtc = "updated_utc";
    private const string UserId = "user_id";
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource source;
    private readonly IWorkspaceActions policy;
    internal PostgresWorkspacePort(NpgsqlDataSource source, IWorkspaceActions policy)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }
    public async ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTimeOffset when = Utc(message.Payload.OccurredUtc, nameof(message));
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
        DateTimeOffset when = Utc(message.Payload.OccurredUtc, nameof(message));
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
    private static async ValueTask<WorkspaceWrite> Start(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            WorkspaceItem? current = await Read(link, lane, command.Identity.ConversationKey, token);
            IReadOnlyList<AccountData> list = await Accounts(link, lane, userId, token);
            WorkspaceData body = Home(list, string.Empty);
            string note = Json(body);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, HomeState, note, command.Payload, command.Payload, when);
            WorkspaceItem? next = current is null
                ? await Add(link, lane, frame, token)
                : await Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                return new WorkspaceWrite(next, current is null);
            }
        }
        throw new InvalidOperationException($"Workspace save exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }
    private static async ValueTask<WorkspaceWrite> Input(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceInputRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            WorkspaceItem? current = await Read(link, lane, command.Identity.ConversationKey, token);
            IReadOnlyList<AccountData> list = await Accounts(link, lane, userId, token);
            WorkspaceData body = current is null ? Home(list, string.Empty) : Data(current.Snapshot.Data);
            WorkspaceData draft = body;
            string state = current?.Snapshot.State ?? HomeState;
            WorkspaceMove move = Move(state, body, command);
            if (move.Entry is not null)
            {
                bool fresh = await Account(link, lane, userId, move.Entry, when, token);
                if (!fresh)
                {
                    move = new WorkspaceMove(ConfirmState, new WorkspaceData([], draft.Name, draft.Currency, draft.Amount, "Account name already exists", string.Empty, false), null);
                }
            }
            if (string.Equals(move.Code, HomeState, StringComparison.Ordinal))
            {
                list = await Accounts(link, lane, userId, token);
                move = new WorkspaceMove(HomeState, Home(list, move.Body.Notice), null);
            }
            string note = Json(move.Body);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, move.Code, note, string.Empty, command.Value, when);
            WorkspaceItem? next = current is null
                ? await Add(link, lane, frame, token)
                : await Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                return new WorkspaceWrite(next, current is null);
            }
        }
        throw new InvalidOperationException($"Workspace input exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }
    private static WorkspaceMove Move(string state, WorkspaceData body, WorkspaceInputRequestedCommand command)
    {
        string kind = command.Kind.Trim();
        return kind switch
        {
            "action" => Act(state, body, command.Value),
            "text" => Text(state, body, command.Value),
            _ => new WorkspaceMove(state, new WorkspaceData(body.Accounts, body.Name, body.Currency, body.Amount, "Input kind is not supported", body.Notice, body.Custom), null)
        };
    }
    private static WorkspaceMove Act(string state, WorkspaceData body, string value)
    {
        string code = value.Trim();
        if (string.Equals(code, "account.cancel", StringComparison.Ordinal))
        {
            return new WorkspaceMove(HomeState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, "Account creation was cancelled", false), null);
        }
        if (string.Equals(state, HomeState, StringComparison.Ordinal))
        {
            return string.Equals(code, "account.add", StringComparison.Ordinal)
                ? new WorkspaceMove(NameState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, string.Empty, false), null)
                : new WorkspaceMove(HomeState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, "Tap Add account to start", false), null);
        }
        if (string.Equals(state, CurrencyState, StringComparison.Ordinal))
        {
            return code switch
            {
                "account.currency.rub" => Currency(body, "RUB"),
                "account.currency.usd" => Currency(body, "USD"),
                "account.currency.eur" => Currency(body, "EUR"),
                "account.currency.other" => new WorkspaceMove(CurrencyState, new WorkspaceData([], body.Name, string.Empty, body.Amount, string.Empty, string.Empty, true), null),
                _ => new WorkspaceMove(CurrencyState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Choose one currency option or send a 3 letter code", string.Empty, body.Custom), null)
            };
        }
        if (string.Equals(state, ConfirmState, StringComparison.Ordinal))
        {
            return string.Equals(code, "account.create", StringComparison.Ordinal)
                ? Create(body)
                : new WorkspaceMove(ConfirmState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Confirm the account or cancel", string.Empty, false), null);
        }
        return new WorkspaceMove(state, new WorkspaceData([], body.Name, body.Currency, body.Amount, "This action is not available", string.Empty, body.Custom), null);
    }
    private static WorkspaceMove Text(string state, WorkspaceData body, string value) => state switch
    {
        HomeState => new WorkspaceMove(HomeState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, "Tap Add account to start", false), null),
        NameState => Draft(body, value),
        CurrencyState => Currency(body, value),
        BalanceState => Amount(body, value),
        ConfirmState => new WorkspaceMove(ConfirmState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Use the buttons to confirm or cancel", string.Empty, false), null),
        _ => new WorkspaceMove(HomeState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, "Tap Add account to start", false), null)
    };
    private static WorkspaceMove Draft(WorkspaceData body, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(NameState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Account name is required", string.Empty, false), null);
        }
        if (body.Amount.HasValue && !string.IsNullOrWhiteSpace(body.Currency))
        {
            return new WorkspaceMove(ConfirmState, new WorkspaceData([], text, body.Currency, body.Amount, string.Empty, string.Empty, false), null);
        }
        if (!string.IsNullOrWhiteSpace(body.Currency))
        {
            return new WorkspaceMove(BalanceState, new WorkspaceData([], text, body.Currency, body.Amount, string.Empty, string.Empty, false), null);
        }
        return new WorkspaceMove(CurrencyState, new WorkspaceData([], text, string.Empty, body.Amount, string.Empty, string.Empty, false), null);
    }
    private static WorkspaceMove Currency(WorkspaceData body, string value)
    {
        string text = value.Trim().ToUpperInvariant();
        bool valid = text.Length == 3 && text.All(char.IsLetter);
        if (!valid)
        {
            return new WorkspaceMove(CurrencyState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Currency code must contain 3 letters", string.Empty, true), null);
        }
        return body.Amount.HasValue
            ? new WorkspaceMove(ConfirmState, new WorkspaceData([], body.Name, text, body.Amount, string.Empty, string.Empty, false), null)
            : new WorkspaceMove(BalanceState, new WorkspaceData([], body.Name, text, body.Amount, string.Empty, string.Empty, false), null);
    }
    private static WorkspaceMove Amount(WorkspaceData body, string value)
    {
        string text = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("\u00A0", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        bool ok = decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount);
        return ok
            ? new WorkspaceMove(ConfirmState, new WorkspaceData([], body.Name, body.Currency, amount, string.Empty, string.Empty, false), null)
            : new WorkspaceMove(BalanceState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Balance must be a number", string.Empty, false), null);
    }
    private static WorkspaceMove Create(WorkspaceData body)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return new WorkspaceMove(NameState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Account name is required", string.Empty, false), null);
        }
        if (string.IsNullOrWhiteSpace(body.Currency))
        {
            return new WorkspaceMove(CurrencyState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Currency code must contain 3 letters", string.Empty, true), null);
        }
        if (!body.Amount.HasValue)
        {
            return new WorkspaceMove(BalanceState, new WorkspaceData([], body.Name, body.Currency, body.Amount, "Balance must be a number", string.Empty, false), null);
        }
        return new WorkspaceMove(HomeState, new WorkspaceData([], string.Empty, string.Empty, null, string.Empty, "Account was created", false), new AccountDraft(body.Name, body.Currency, body.Amount.Value));
    }
    private static WorkspaceData Home(IReadOnlyList<AccountData> list, string notice) => new(list, string.Empty, string.Empty, null, string.Empty, notice, false);
    private static WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return new WorkspaceData(item?.Accounts ?? [], item?.Name ?? string.Empty, item?.Currency ?? string.Empty, item?.Amount, item?.Error ?? string.Empty, item?.Notice ?? string.Empty, item?.Custom ?? false);
    }
    private static string Json(WorkspaceData item) => JsonSerializer.Serialize(item, json);
    private static DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        return value.Offset == TimeSpan.Zero ? value : throw new ArgumentException("Workspace occurrence time must be UTC", name);
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
    private static async ValueTask<WorkspaceItem?> Read(NpgsqlConnection link, NpgsqlTransaction lane, string key, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id, state_code, state_data, revision from finance.workspace where conversation_key = @conversation_key for update", link, lane);
        note.Parameters.AddWithValue("conversation_key", key);
        return await Item(note, false, token);
    }
    private static async ValueTask<WorkspaceItem?> Add(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceFrame frame, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.workspace(id, user_id, conversation_key, state_code, state_data, revision, entry_payload, last_payload, created_utc, opened_utc, updated_utc) values (@id, @user_id, @conversation_key, @state_code, @state_data, @revision, @entry_payload, @last_payload, @created_utc, @opened_utc, @updated_utc) on conflict (conversation_key) do nothing returning id, state_code, state_data, revision", link, lane);
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
    private static async ValueTask<WorkspaceItem?> Write(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceMark mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new("update finance.workspace set user_id = @user_id, state_code = @state_code, state_data = @state_data, last_payload = @last_payload, revision = revision + 1, opened_utc = @opened_utc, updated_utc = @updated_utc where id = @id and revision = @revision returning id, state_code, state_data, revision", link, lane);
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
    private static async ValueTask<bool> Account(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, AccountDraft draft, DateTimeOffset when, CancellationToken token)
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
    private static async ValueTask<IReadOnlyList<AccountData>> Accounts(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select name, currency_code, current_amount from finance.account where user_id = @user_id order by created_utc, name", link, lane);
        note.Parameters.AddWithValue(UserId, userId);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        List<AccountData> list = [];
        while (await row.ReadAsync(token))
        {
            list.Add(new AccountData(row.GetString(0), row.GetString(1), row.GetDecimal(2)));
        }
        return list;
    }
    private async ValueTask Outbox<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, WorkspaceItem item, WorkspaceViewNote note, CancellationToken token) where TMessage : class
    {
        var state = new WorkspaceState(item.Snapshot.State, item.Snapshot.Data, item.Snapshot.Revision);
        var view = new WorkspaceView(note.Identity, note.Profile, state, policy.Codes(state.Code, Data(state.Data).Custom), note.IsNewUser, note.IsNewWorkspace, note.When);
        var body = new WorkspaceViewRequestedCommand(view.Identity, view.Profile, new WorkspaceViewFrame(view.State.Code, view.State.Data, view.Actions), new WorkspaceViewFreshness(view.IsNewUser, view.IsNewWorkspace), view.OccurredUtc);
        var envelope = new MessageEnvelope<WorkspaceViewRequestedCommand>(Guid.CreateVersion7(), ViewContract, note.When, new MessageContext(message.Context.CorrelationId, message.MessageId.ToString(), $"{message.Context.IdempotencyKey}:workspace-view"), ViewSource, body);
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
    private static async ValueTask<WorkspaceItem?> Item(NpgsqlCommand note, bool isNew, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? new WorkspaceItem(row.GetGuid(0), new WorkspaceSnapshot(row.GetString(1), row.GetString(2), row.GetInt64(3), isNew)) : null;
    }
    private sealed record WorkspaceMove
    {
        internal WorkspaceMove(string code, WorkspaceData body, AccountDraft? entry)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Entry = entry;
        }
        internal string Code { get; }
        internal WorkspaceData Body { get; }
        internal AccountDraft? Entry { get; }
    }
    private sealed record AccountDraft
    {
        internal AccountDraft(string title, string unit, decimal total)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Total = total;
        }
        internal string Title { get; }
        internal string Unit { get; }
        internal decimal Total { get; }
    }
    private sealed record WorkspaceFrame
    {
        internal WorkspaceFrame(Guid user, string room, string state, string body, string entry, string last, DateTimeOffset when)
        {
            UserValue = user;
            Room = room ?? throw new ArgumentNullException(nameof(room));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Last = last ?? throw new ArgumentNullException(nameof(last));
            When = when;
        }
        internal Guid UserValue { get; }
        internal string Room { get; }
        internal string State { get; }
        internal string Body { get; }
        internal string Entry { get; }
        internal string Last { get; }
        internal DateTimeOffset When { get; }
    }
    private sealed record WorkspaceMark
    {
        internal WorkspaceMark(Guid id, long revision, WorkspaceFrame frame)
        {
            IdValue = id;
            Revision = revision;
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }
        internal Guid IdValue { get; }
        internal long Revision { get; }
        internal WorkspaceFrame Frame { get; }
    }
    private sealed record WorkspaceWrite
    {
        internal WorkspaceWrite(WorkspaceItem item, bool isNew)
        {
            State = item ?? throw new ArgumentNullException(nameof(item));
            IsNew = isNew;
        }
        internal WorkspaceItem State { get; }
        internal bool IsNew { get; }
    }
    private sealed record WorkspaceViewNote
    {
        internal WorkspaceViewNote(WorkspaceIdentity identity, WorkspaceProfile profile, bool isNewUser, bool isNewWorkspace, DateTimeOffset when)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            IsNewUser = isNewUser;
            IsNewWorkspace = isNewWorkspace;
            When = when;
        }
        internal WorkspaceIdentity Identity { get; }
        internal WorkspaceProfile Profile { get; }
        internal bool IsNewUser { get; }
        internal bool IsNewWorkspace { get; }
        internal DateTimeOffset When { get; }
    }
}
