using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Runtime.Ports;
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Inbox;

internal sealed class PostgresInboxPort : IInboxPort
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource data;
    internal PostgresInboxPort(NpgsqlDataSource data) => this.data = data ?? throw new ArgumentNullException(nameof(data));
    public async ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, json);
        string text = Encoding.UTF8.GetString(body);
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand note = new("insert into finance.inbox_message(message_id, contract, source, correlation_id, causation_id, idempotency_key, payload, received_utc, processed_utc, attempt) values (@message_id, @contract, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @received_utc, @processed_utc, @attempt) on conflict do nothing", link);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("source", message.Source);
        note.Parameters.AddWithValue("correlation_id", message.Context.CorrelationId);
        note.Parameters.AddWithValue("causation_id", message.Context.CausationId);
        note.Parameters.AddWithValue("idempotency_key", message.Context.IdempotencyKey);
        note.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, text);
        note.Parameters.AddWithValue("received_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("processed_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("attempt", 1);
        _ = await note.ExecuteNonQueryAsync(token);
    }
}
