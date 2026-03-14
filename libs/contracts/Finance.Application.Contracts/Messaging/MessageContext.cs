namespace Finance.Application.Contracts.Messaging;

/// <summary>
/// Represents the correlation metadata carried by a transport envelope.
/// </summary>
public sealed record MessageContext
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causationId">The causation identifier.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    public MessageContext(string correlationId, string causationId, string idempotencyKey)
    {
        CorrelationId = !string.IsNullOrWhiteSpace(correlationId) ? correlationId : throw new ArgumentException("Message correlation id is required", nameof(correlationId));
        CausationId = !string.IsNullOrWhiteSpace(causationId) ? causationId : throw new ArgumentException("Message causation id is required", nameof(causationId));
        IdempotencyKey = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : throw new ArgumentException("Message idempotency key is required", nameof(idempotencyKey));
    }
    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string CorrelationId { get; init; }
    /// <summary>
    /// Gets the causation identifier.
    /// </summary>
    public string CausationId { get; init; }
    /// <summary>
    /// Gets the idempotency key.
    /// </summary>
    public string IdempotencyKey { get; init; }
}
