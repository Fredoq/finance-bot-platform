namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the immutable workspace identity across service boundaries.
/// </summary>
/// <param name="ActorKey">The opaque actor key.</param>
/// <param name="ConversationKey">The opaque conversation key.</param>
public sealed record WorkspaceIdentity(
    string ActorKey,
    string ConversationKey)
{
    /// <summary>
    /// Gets the opaque actor key for the workspace request.
    /// </summary>
    public string ActorKey { get; init; } = !string.IsNullOrWhiteSpace(ActorKey) ? ActorKey : throw new ArgumentException("Workspace actor key is required", nameof(ActorKey));
    /// <summary>
    /// Gets the opaque conversation key for the workspace request.
    /// </summary>
    public string ConversationKey { get; init; } = !string.IsNullOrWhiteSpace(ConversationKey) ? ConversationKey : throw new ArgumentException("Workspace conversation key is required", nameof(ConversationKey));
}
