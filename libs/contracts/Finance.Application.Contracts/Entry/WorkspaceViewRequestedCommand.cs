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
    /// <param name="state">The current workspace state code.</param>
    /// <param name="actions">The supported action codes.</param>
    /// <param name="isNewUser">Indicates whether the user was created by the workflow.</param>
    /// <param name="isNewWorkspace">Indicates whether the workspace was created by the workflow.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    public WorkspaceViewRequestedCommand(WorkspaceIdentity identity, WorkspaceProfile profile, string state, IReadOnlyList<string> actions, bool isNewUser, bool isNewWorkspace, DateTimeOffset occurredUtc)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(actions);
        string[] list = actions.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        Identity = identity;
        Profile = profile;
        State = !string.IsNullOrWhiteSpace(state) ? state.Trim() : throw new ArgumentException("Workspace state is required", nameof(state));
        Actions = list.Length > 0 ? Array.AsReadOnly(list) : throw new ArgumentException("Workspace actions are required", nameof(actions));
        IsNewUser = isNewUser;
        IsNewWorkspace = isNewWorkspace;
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
    /// Gets the current workspace state code.
    /// </summary>
    public string State { get; }
    /// <summary>
    /// Gets the supported action codes.
    /// </summary>
    public IReadOnlyList<string> Actions { get; }
    /// <summary>
    /// Gets a value indicating whether the user was created by the workflow.
    /// </summary>
    public bool IsNewUser { get; }
    /// <summary>
    /// Gets a value indicating whether the workspace was created by the workflow.
    /// </summary>
    public bool IsNewWorkspace { get; }
    /// <summary>
    /// Gets the UTC time when the view request occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; }
}
