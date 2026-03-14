namespace Finance.Application.Contracts.Messaging;

/// <summary>
/// Represents a transport envelope for an application contract.
/// </summary>
/// <typeparam name="TMessage">The payload type.</typeparam>
/// <param name="MessageId">The transport message identifier.</param>
/// <param name="Contract">The contract name.</param>
/// <param name="OccurredUtc">The UTC occurrence time.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="CausationId">The causation identifier.</param>
/// <param name="IdempotencyKey">The idempotency key.</param>
/// <param name="Source">The source name.</param>
/// <param name="Payload">The message payload.</param>
public sealed record MessageEnvelope<TMessage>(
    Guid MessageId,
    string Contract,
    DateTimeOffset OccurredUtc,
    string CorrelationId,
    string CausationId,
    string IdempotencyKey,
    string Source,
    TMessage Payload)
    where TMessage : class
{
    /// <summary>
    /// Gets the transport message identifier.
    /// </summary>
    public Guid MessageId { get; init; } = MessageId != Guid.Empty ? MessageId : throw new ArgumentOutOfRangeException(nameof(MessageId));
    /// <summary>
    /// Gets the contract name carried by the envelope.
    /// </summary>
    public string Contract { get; init; } = !string.IsNullOrWhiteSpace(Contract) ? Contract : throw new ArgumentException("Envelope contract is required", nameof(Contract));
    /// <summary>
    /// Gets the UTC time when the envelope occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; } = OccurredUtc != default ? OccurredUtc : throw new ArgumentOutOfRangeException(nameof(OccurredUtc));
    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; init; } = !string.IsNullOrWhiteSpace(CorrelationId) ? CorrelationId : throw new ArgumentException("Envelope correlation id is required", nameof(CorrelationId));
    /// <summary>
    /// Gets the causation identifier.
    /// </summary>
    public string CausationId { get; init; } = !string.IsNullOrWhiteSpace(CausationId) ? CausationId : throw new ArgumentException("Envelope causation id is required", nameof(CausationId));
    /// <summary>
    /// Gets the idempotency key.
    /// </summary>
    public string IdempotencyKey { get; init; } = !string.IsNullOrWhiteSpace(IdempotencyKey) ? IdempotencyKey : throw new ArgumentException("Envelope idempotency key is required", nameof(IdempotencyKey));
    /// <summary>
    /// Gets the envelope source.
    /// </summary>
    public string Source { get; init; } = !string.IsNullOrWhiteSpace(Source) ? Source : throw new ArgumentException("Envelope source is required", nameof(Source));
    /// <summary>
    /// Gets the payload instance.
    /// </summary>
    public TMessage Payload { get; init; } = Payload ?? throw new ArgumentNullException(nameof(Payload));
}
