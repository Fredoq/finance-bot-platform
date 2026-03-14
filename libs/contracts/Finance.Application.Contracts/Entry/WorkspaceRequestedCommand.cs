namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents a request to open or restore the user workspace.
/// </summary>
public sealed record WorkspaceRequestedCommand
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="identity">The workspace identity.</param>
    /// <param name="profile">The workspace profile.</param>
    /// <param name="payload">The start payload text.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    public WorkspaceRequestedCommand(WorkspaceIdentity identity, WorkspaceProfile profile, string payload, DateTimeOffset occurredUtc)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        OccurredUtc = occurredUtc != default ? occurredUtc : throw new ArgumentOutOfRangeException(nameof(occurredUtc));
    }
    /// <summary>
    /// Gets the immutable workspace identity details.
    /// </summary>
    public WorkspaceIdentity Identity { get; init; }
    /// <summary>
    /// Gets the immutable profile details.
    /// </summary>
    public WorkspaceProfile Profile { get; init; }
    /// <summary>
    /// Gets the `/start` payload text.
    /// </summary>
    public string Payload { get; init; }
    /// <summary>
    /// Gets the UTC time when the workspace request occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; }
}
