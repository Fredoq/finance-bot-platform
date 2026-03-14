namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the immutable workspace identity across service boundaries.
/// </summary>
public sealed record WorkspaceIdentity
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="actorKey">The opaque actor key.</param>
    /// <param name="conversationKey">The opaque conversation key.</param>
    public WorkspaceIdentity(string actorKey, string conversationKey)
    {
        ActorKey = !string.IsNullOrWhiteSpace(actorKey) ? actorKey : throw new ArgumentException("Workspace actor key is required", nameof(actorKey));
        ConversationKey = !string.IsNullOrWhiteSpace(conversationKey) ? conversationKey : throw new ArgumentException("Workspace conversation key is required", nameof(conversationKey));
    }
    /// <summary>
    /// Gets the opaque actor key for the workspace request.
    /// </summary>
    public string ActorKey { get; init; }
    /// <summary>
    /// Gets the opaque conversation key for the workspace request.
    /// </summary>
    public string ConversationKey { get; init; }
}
