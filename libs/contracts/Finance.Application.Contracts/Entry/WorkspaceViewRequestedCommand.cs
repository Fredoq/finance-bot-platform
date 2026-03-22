namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents a request to display the current workspace view.
/// </summary>
public sealed record WorkspaceViewRequestedCommand
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="identity">The workspace identity.</param>
    /// <param name="profile">The workspace profile.</param>
    /// <param name="frame">The visible workspace frame.</param>
    /// <param name="freshness">The workspace novelty flags.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    public WorkspaceViewRequestedCommand(WorkspaceIdentity identity, WorkspaceProfile profile, WorkspaceViewFrame frame, WorkspaceViewFreshness freshness, DateTimeOffset occurredUtc)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        Freshness = freshness ?? throw new ArgumentNullException(nameof(freshness));
        ArgumentOutOfRangeException.ThrowIfEqual(occurredUtc, default);
        OccurredUtc = occurredUtc.Offset == TimeSpan.Zero ? occurredUtc : throw new ArgumentException("Workspace occurrence time must be UTC", nameof(occurredUtc));
    }
    /// <summary>
    /// Gets the workspace identity details.
    /// </summary>
    public WorkspaceIdentity Identity { get; }
    /// <summary>
    /// Gets the profile details.
    /// </summary>
    public WorkspaceProfile Profile { get; }
    /// <summary>
    /// Gets the visible workspace frame.
    /// </summary>
    public WorkspaceViewFrame Frame { get; }
    /// <summary>
    /// Gets the novelty flags.
    /// </summary>
    public WorkspaceViewFreshness Freshness { get; }
    /// <summary>
    /// Gets the UTC time when the view request occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; }
}
