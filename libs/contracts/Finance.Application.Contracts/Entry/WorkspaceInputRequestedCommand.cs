namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents a request to apply user input to the current workspace.
/// </summary>
public sealed record WorkspaceInputRequestedCommand
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="identity">The workspace identity.</param>
    /// <param name="profile">The workspace profile.</param>
    /// <param name="kind">The input kind.</param>
    /// <param name="value">The input value.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    public WorkspaceInputRequestedCommand(WorkspaceIdentity identity, WorkspaceProfile profile, string kind, string value, DateTimeOffset occurredUtc)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(profile);
        Identity = identity;
        Profile = profile;
        Kind = !string.IsNullOrWhiteSpace(kind) ? kind.Trim() : throw new ArgumentException("Workspace input kind is required", nameof(kind));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        ArgumentOutOfRangeException.ThrowIfEqual(occurredUtc, default);
        OccurredUtc = occurredUtc.Offset == TimeSpan.Zero ? occurredUtc : throw new ArgumentException("Workspace occurrence time must be UTC", nameof(occurredUtc));
    }
    /// <summary>
    /// Gets the workspace identity details.
    /// </summary>
    public WorkspaceIdentity Identity { get; init; }
    /// <summary>
    /// Gets the profile details.
    /// </summary>
    public WorkspaceProfile Profile { get; init; }
    /// <summary>
    /// Gets the input kind.
    /// </summary>
    public string Kind { get; init; }
    /// <summary>
    /// Gets the input value.
    /// </summary>
    public string Value { get; init; }
    /// <summary>
    /// Gets the UTC time when the input occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; }
}
