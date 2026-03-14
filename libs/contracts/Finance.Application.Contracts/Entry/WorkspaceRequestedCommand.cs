namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents a request to open or restore the user workspace.
/// </summary>
/// <param name="Identity">The workspace identity.</param>
/// <param name="Profile">The workspace profile.</param>
/// <param name="Payload">The start payload text.</param>
/// <param name="OccurredUtc">The UTC occurrence time.</param>
public sealed record WorkspaceRequestedCommand(
    WorkspaceIdentity Identity,
    WorkspaceProfile Profile,
    string Payload,
    DateTimeOffset OccurredUtc)
{
    /// <summary>
    /// Gets the immutable workspace identity details.
    /// </summary>
    public WorkspaceIdentity Identity { get; init; } = Identity ?? throw new ArgumentNullException(nameof(Identity));
    /// <summary>
    /// Gets the immutable profile details.
    /// </summary>
    public WorkspaceProfile Profile { get; init; } = Profile ?? throw new ArgumentNullException(nameof(Profile));
    /// <summary>
    /// Gets the `/start` payload text.
    /// </summary>
    public string Payload { get; init; } = Payload ?? throw new ArgumentNullException(nameof(Payload));
    /// <summary>
    /// Gets the UTC time when the workspace request occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; } = OccurredUtc != default ? OccurredUtc : throw new ArgumentOutOfRangeException(nameof(OccurredUtc));
}
