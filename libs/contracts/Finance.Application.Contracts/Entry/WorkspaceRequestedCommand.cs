namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents a request to open or restore the user workspace.
/// Example:
/// <code>
/// var command = new WorkspaceRequestedCommand(
///     "actor-key",
///     "conversation-key",
///     "Alex Doe",
///     "en",
///     "campaign-42",
///     DateTimeOffset.UtcNow);
/// </code>
/// </summary>
public sealed record WorkspaceRequestedCommand(
    string ActorKey,
    string ConversationKey,
    string Name,
    string Locale,
    string Payload,
    DateTimeOffset OccurredUtc);
