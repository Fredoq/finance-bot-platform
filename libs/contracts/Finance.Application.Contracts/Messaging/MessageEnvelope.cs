namespace Finance.Application.Contracts.Messaging;

/// <summary>
/// Represents a transport envelope for an application contract.
/// </summary>
/// <typeparam name="TMessage">The payload type.</typeparam>
public sealed record MessageEnvelope<TMessage> where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="messageId">The transport message identifier.</param>
    /// <param name="contract">The contract name.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    /// <param name="context">The message context.</param>
    /// <param name="source">The source name.</param>
    /// <param name="payload">The message payload.</param>
    public MessageEnvelope(Guid messageId, string contract, DateTimeOffset occurredUtc, MessageContext context, string source, TMessage payload)
    {
        MessageId = messageId != Guid.Empty ? messageId : throw new ArgumentOutOfRangeException(nameof(messageId));
        Contract = !string.IsNullOrWhiteSpace(contract) ? contract : throw new ArgumentException("Envelope contract is required", nameof(contract));
        OccurredUtc = occurredUtc != default ? occurredUtc : throw new ArgumentOutOfRangeException(nameof(occurredUtc));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Source = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentException("Envelope source is required", nameof(source));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
    /// <summary>
    /// Gets the transport message identifier.
    /// </summary>
    public Guid MessageId { get; init; }
    /// <summary>
    /// Gets the contract name carried by the envelope.
    /// </summary>
    public string Contract { get; init; }
    /// <summary>
    /// Gets the UTC time when the envelope occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; }
    /// <summary>
    /// Gets the message context.
    /// </summary>
    public MessageContext Context { get; init; }
    /// <summary>
    /// Gets the envelope source.
    /// </summary>
    public string Source { get; init; }
    /// <summary>
    /// Gets the payload instance.
    /// </summary>
    public TMessage Payload { get; init; }
}
