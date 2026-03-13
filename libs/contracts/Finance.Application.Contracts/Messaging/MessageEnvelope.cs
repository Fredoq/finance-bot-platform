namespace Finance.Application.Contracts.Messaging;

/// <summary>
/// Represents a transport envelope for an application contract.
/// Example:
/// <code>
/// var command = new WorkspaceRequestedCommand("actor", "conversation", "Alex", "en", "", DateTimeOffset.UtcNow);
/// var envelope = new MessageEnvelope&lt;WorkspaceRequestedCommand&gt;(
///     Guid.CreateVersion7(),
///     "workspace.requested",
///     DateTimeOffset.UtcNow,
///     "trace-1",
///     "edge-update-1",
///     "edge-update-1",
///     "telegram-gateway",
///     command);
/// </code>
/// </summary>
/// <typeparam name="TMessage">The payload type that represents the application contract.</typeparam>
public sealed record MessageEnvelope<TMessage>(
    Guid MessageId,
    string Contract,
    DateTimeOffset OccurredUtc,
    string CorrelationId,
    string CausationId,
    string IdempotencyKey,
    string Source,
    TMessage Payload)
    where TMessage : class;
