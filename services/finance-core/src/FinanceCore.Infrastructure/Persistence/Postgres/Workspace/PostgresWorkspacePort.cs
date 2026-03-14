using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Workspace.Ports;
using FinanceCore.Domain.Workspace.Models;
using FinanceCore.Domain.Workspace.Policies;
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class PostgresWorkspacePort : IWorkspacePort
{
    private const string Home = "home";
    private const string Contract = "workspace.view.requested";
    private const string RoutingKey = "workspace.view.requested";
    private const string Source = "finance-core";
    private const string UpdatedUtc = "updated_utc";
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource data;
    private readonly IWorkspaceActions actions;
    internal PostgresWorkspacePort(NpgsqlDataSource data, IWorkspaceActions actions)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }
    public async ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        string inbound = JsonSerializer.Serialize(message, json);
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync(token);
        bool fresh = await Inbox(link, lane, message, inbound, token);
        if (!fresh)
        {
            await lane.CommitAsync(token);
            return;
        }
        (Guid userId, bool isNewUser) = await User(link, lane, message, token);
        WorkspaceItem state = await Workspace(link, lane, userId, message, token);
        var note = new WorkspaceState(state.Snapshot.State, state.Snapshot.Data, state.Snapshot.Revision);
        var view = new WorkspaceView(message.Payload.Identity, message.Payload.Profile, note, actions.Codes(), isNewUser, state.Snapshot.IsNew, message.Payload.OccurredUtc);
        var item = new WorkspaceViewRequestedCommand(view.Identity, view.Profile, view.State.Code, view.Actions, view.IsNewUser, view.IsNewWorkspace, view.OccurredUtc);
        var messageId = Guid.CreateVersion7();
        var envelope = new MessageEnvelope<WorkspaceViewRequestedCommand>(messageId, Contract, view.OccurredUtc, new MessageContext(message.Context.CorrelationId, message.MessageId.ToString(), $"{message.Context.IdempotencyKey}:workspace-view"), Source, item);
        string outbound = JsonSerializer.Serialize(envelope, json);
        await Outbox(link, lane, messageId, outbound, message, view, token);
        await Processed(link, lane, message, token);
        await lane.CommitAsync(token);
    }
    private static async ValueTask<bool> Inbox(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<WorkspaceRequestedCommand> message, string payload, CancellationToken token)
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
    private static async ValueTask<(Guid UserId, bool IsNewUser)> User(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        await using NpgsqlCommand add = new("insert into finance.user_account(id, actor_key, name, locale, created_utc, updated_utc) values (@id, @actor_key, @name, @locale, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        add.Parameters.AddWithValue("id", Guid.CreateVersion7());
        add.Parameters.AddWithValue("actor_key", message.Payload.Identity.ActorKey);
        add.Parameters.AddWithValue("name", message.Payload.Profile.Name);
        add.Parameters.AddWithValue("locale", message.Payload.Profile.Locale);
        add.Parameters.AddWithValue("created_utc", message.Payload.OccurredUtc);
        add.Parameters.AddWithValue("updated_utc", message.Payload.OccurredUtc);
        Guid? userId = await UserId(add, token);
        if (userId.HasValue)
        {
            return (userId.Value, true);
        }
        await using NpgsqlCommand note = new("update finance.user_account set name = @name, locale = @locale, updated_utc = @updated_utc where actor_key = @actor_key returning id", link, lane);
        note.Parameters.AddWithValue("actor_key", message.Payload.Identity.ActorKey);
        note.Parameters.AddWithValue("name", message.Payload.Profile.Name);
        note.Parameters.AddWithValue("locale", message.Payload.Profile.Locale);
        note.Parameters.AddWithValue(UpdatedUtc, message.Payload.OccurredUtc);
        userId = await UserId(note, token);
        if (userId.HasValue)
        {
            return (userId.Value, false);
        }
        throw new InvalidOperationException("User upsert failed");
    }
    private static async ValueTask<WorkspaceItem> Workspace(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        await using NpgsqlCommand add = new("insert into finance.workspace(id, user_id, conversation_key, state_code, state_data, revision, entry_payload, last_payload, created_utc, opened_utc, updated_utc) values (@id, @user_id, @conversation_key, @state_code, @state_data, @revision, @entry_payload, @last_payload, @created_utc, @opened_utc, @updated_utc) on conflict do nothing returning id, state_code, state_data, revision", link, lane);
        add.Parameters.AddWithValue("id", Guid.CreateVersion7());
        add.Parameters.AddWithValue("user_id", userId);
        add.Parameters.AddWithValue("conversation_key", message.Payload.Identity.ConversationKey);
        add.Parameters.AddWithValue("state_code", Home);
        add.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, "{}");
        add.Parameters.AddWithValue("revision", 1L);
        add.Parameters.AddWithValue("entry_payload", message.Payload.Payload);
        add.Parameters.AddWithValue("last_payload", message.Payload.Payload);
        add.Parameters.AddWithValue("created_utc", message.Payload.OccurredUtc);
        add.Parameters.AddWithValue("opened_utc", message.Payload.OccurredUtc);
        add.Parameters.AddWithValue("updated_utc", message.Payload.OccurredUtc);
        WorkspaceItem? item = await WorkspaceItem(add, true, token);
        if (item is not null)
        {
            return item;
        }
        await using NpgsqlCommand note = new("update finance.workspace set user_id = @user_id, last_payload = @last_payload, revision = revision + 1, opened_utc = @opened_utc, updated_utc = @updated_utc where conversation_key = @conversation_key returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("user_id", userId);
        note.Parameters.AddWithValue("conversation_key", message.Payload.Identity.ConversationKey);
        note.Parameters.AddWithValue("last_payload", message.Payload.Payload);
        note.Parameters.AddWithValue("opened_utc", message.Payload.OccurredUtc);
        note.Parameters.AddWithValue(UpdatedUtc, message.Payload.OccurredUtc);
        item = await WorkspaceItem(note, false, token);
        if (item is not null)
        {
            return item;
        }
        throw new InvalidOperationException("Workspace upsert failed");
    }
    private static async ValueTask Outbox(NpgsqlConnection link, NpgsqlTransaction lane, Guid messageId, string payload, MessageEnvelope<WorkspaceRequestedCommand> message, WorkspaceView view, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.outbox_message(message_id, contract, routing_key, source, correlation_id, causation_id, idempotency_key, payload, occurred_utc, created_utc, published_utc, attempt, error) values (@message_id, @contract, @routing_key, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @occurred_utc, @created_utc, @published_utc, @attempt, @error) on conflict do nothing", link, lane);
        note.Parameters.AddWithValue("message_id", messageId);
        note.Parameters.AddWithValue("contract", Contract);
        note.Parameters.AddWithValue("routing_key", RoutingKey);
        note.Parameters.AddWithValue("source", Source);
        note.Parameters.AddWithValue("correlation_id", message.Context.CorrelationId);
        note.Parameters.AddWithValue("causation_id", message.MessageId.ToString());
        note.Parameters.AddWithValue("idempotency_key", $"{message.Context.IdempotencyKey}:workspace-view");
        note.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
        note.Parameters.AddWithValue("occurred_utc", view.OccurredUtc);
        note.Parameters.AddWithValue("created_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("published_utc", DBNull.Value);
        note.Parameters.AddWithValue("attempt", 0);
        note.Parameters.AddWithValue("error", string.Empty);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Outbox insert failed");
        }
    }
    private static async ValueTask Processed(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
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
    private static async ValueTask<Guid?> UserId(NpgsqlCommand note, CancellationToken token)
    {
        await using NpgsqlDataReader data = await note.ExecuteReaderAsync(token);
        return await data.ReadAsync(token) ? data.GetGuid(0) : null;
    }
    private static async ValueTask<WorkspaceItem?> WorkspaceItem(NpgsqlCommand note, bool isNew, CancellationToken token)
    {
        await using NpgsqlDataReader data = await note.ExecuteReaderAsync(token);
        return await data.ReadAsync(token) ? new WorkspaceItem(data.GetGuid(0), new WorkspaceSnapshot(data.GetString(1), data.GetString(2), data.GetInt64(3), isNew)) : null;
    }
}
