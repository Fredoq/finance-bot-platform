namespace FinanceCore.Infrastructure.Persistence.Postgres.Outbox;

internal sealed record OutboxHead
{
    internal OutboxHead(Guid messageId, string contract, string routingKey, DateTimeOffset occurredUtc)
    {
        MessageId = messageId;
        Contract = !string.IsNullOrWhiteSpace(contract) ? contract : throw new ArgumentException("Outbox contract is required", nameof(contract));
        RoutingKey = !string.IsNullOrWhiteSpace(routingKey) ? routingKey : throw new ArgumentException("Outbox routing key is required", nameof(routingKey));
        OccurredUtc = occurredUtc != default ? occurredUtc : throw new ArgumentOutOfRangeException(nameof(occurredUtc));
    }
    internal Guid MessageId { get; }
    internal string Contract { get; }
    internal string RoutingKey { get; }
    internal DateTimeOffset OccurredUtc { get; }
}

internal sealed record OutboxMark
{
    internal OutboxMark(string source, string correlationId, string causationId)
    {
        Source = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentException("Outbox source is required", nameof(source));
        CorrelationId = !string.IsNullOrWhiteSpace(correlationId) ? correlationId : throw new ArgumentException("Outbox correlation id is required", nameof(correlationId));
        CausationId = !string.IsNullOrWhiteSpace(causationId) ? causationId : throw new ArgumentException("Outbox causation id is required", nameof(causationId));
    }
    internal string Source { get; }
    internal string CorrelationId { get; }
    internal string CausationId { get; }
}

internal sealed record OutboxBody
{
    internal OutboxBody(ReadOnlyMemory<byte> payload, int attempt)
    {
        Payload = !payload.IsEmpty ? payload : throw new ArgumentException("Outbox payload is required", nameof(payload));
        Attempt = attempt >= 0 ? attempt : throw new ArgumentOutOfRangeException(nameof(attempt));
    }
    internal ReadOnlyMemory<byte> Payload { get; }
    internal int Attempt { get; }
}

internal sealed record OutboxItem
{
    internal OutboxItem(OutboxHead head, OutboxMark mark, OutboxBody body)
    {
        Head = head ?? throw new ArgumentNullException(nameof(head));
        Mark = mark ?? throw new ArgumentNullException(nameof(mark));
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }
    internal OutboxHead Head { get; }
    internal OutboxMark Mark { get; }
    internal OutboxBody Body { get; }
}
