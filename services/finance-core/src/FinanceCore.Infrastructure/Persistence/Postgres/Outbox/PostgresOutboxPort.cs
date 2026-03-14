using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Runtime.Ports;
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Outbox;

internal sealed class PostgresOutboxPort : IOutboxPort
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource data;
    internal PostgresOutboxPort(NpgsqlDataSource data) => this.data = data ?? throw new ArgumentNullException(nameof(data));
    public async ValueTask Save<TMessage>(MessageEnvelope<TMessage> message, string routingKey, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        string text = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(message, json));
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand note = new("insert into finance.outbox_message(message_id, contract, routing_key, source, correlation_id, causation_id, idempotency_key, payload, occurred_utc, created_utc, published_utc, attempt, error) values (@message_id, @contract, @routing_key, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @occurred_utc, @created_utc, @published_utc, @attempt, @error) on conflict do nothing", link);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("routing_key", routingKey);
        note.Parameters.AddWithValue("source", message.Source);
        note.Parameters.AddWithValue("correlation_id", message.Context.CorrelationId);
        note.Parameters.AddWithValue("causation_id", message.Context.CausationId);
        note.Parameters.AddWithValue("idempotency_key", message.Context.IdempotencyKey);
        note.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, text);
        note.Parameters.AddWithValue("occurred_utc", message.OccurredUtc);
        note.Parameters.AddWithValue("created_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("published_utc", DBNull.Value);
        note.Parameters.AddWithValue("attempt", 0);
        note.Parameters.AddWithValue("error", string.Empty);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    internal async ValueTask<IReadOnlyList<OutboxItem>> Items(int size, CancellationToken token)
    {
        var list = new List<OutboxItem>();
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand note = new("select message_id, contract, routing_key, source, correlation_id, causation_id, payload, occurred_utc, attempt from finance.outbox_message where published_utc is null order by created_utc limit @size", link);
        note.Parameters.AddWithValue("size", size);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        while (await row.ReadAsync(token))
        {
            list.Add(new OutboxItem(row.GetGuid(0), row.GetString(1), row.GetString(2), row.GetString(3), row.GetString(4), row.GetString(5), Encoding.UTF8.GetBytes(row.GetString(6)), await row.GetFieldValueAsync<DateTimeOffset>(7, token), row.GetInt32(8)));
        }
        return list;
    }
    internal async ValueTask Mark(Guid messageId, CancellationToken token)
    {
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand note = new("update finance.outbox_message set published_utc = @published_utc where message_id = @message_id", link);
        note.Parameters.AddWithValue("published_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("message_id", messageId);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    internal async ValueTask Fail(Guid messageId, string error, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        await using NpgsqlConnection link = await data.OpenConnectionAsync(token);
        await using NpgsqlCommand note = new("update finance.outbox_message set attempt = attempt + 1, error = @error where message_id = @message_id", link);
        note.Parameters.AddWithValue("message_id", messageId);
        note.Parameters.AddWithValue("error", error);
        _ = await note.ExecuteNonQueryAsync(token);
    }
}
