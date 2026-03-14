using Finance.Application.Contracts.Entry;
using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Domain.Workspace.Models;

/// <summary>
/// Represents the semantic workspace view that is sent downstream.
/// </summary>
public sealed record WorkspaceView
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="identity">The workspace identity.</param>
    /// <param name="profile">The workspace profile.</param>
    /// <param name="state">The current workspace state.</param>
    /// <param name="actions">The supported action codes.</param>
    /// <param name="isNewUser">Indicates whether the user was created by the workflow.</param>
    /// <param name="isNewWorkspace">Indicates whether the workspace was created by the workflow.</param>
    /// <param name="occurredUtc">The UTC occurrence time.</param>
    public WorkspaceView(WorkspaceIdentity identity, WorkspaceProfile profile, WorkspaceState state, IReadOnlyList<string> actions, bool isNewUser, bool isNewWorkspace, DateTimeOffset occurredUtc)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actions);
        string[] list = actions.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        Identity = identity;
        Profile = profile;
        State = state;
        Actions = list.Length > 0 ? Array.AsReadOnly(list) : throw new ArgumentException("Workspace actions are required", nameof(actions));
        IsNewUser = isNewUser;
        IsNewWorkspace = isNewWorkspace;
        ArgumentOutOfRangeException.ThrowIfEqual(occurredUtc, default);
        OccurredUtc = occurredUtc.Offset == TimeSpan.Zero ? occurredUtc : throw new ArgumentException("Workspace occurrence time must be UTC", nameof(occurredUtc));
    }
    /// <summary>
    /// Gets the workspace identity.
    /// </summary>
    public WorkspaceIdentity Identity { get; }
    /// <summary>
    /// Gets the workspace profile.
    /// </summary>
    public WorkspaceProfile Profile { get; }
    /// <summary>
    /// Gets the current workspace state.
    /// </summary>
    public WorkspaceState State { get; }
    /// <summary>
    /// Gets the supported action codes.
    /// </summary>
    public IReadOnlyList<string> Actions { get; }
    /// <summary>
    /// Gets a value indicating whether the user is new.
    /// </summary>
    public bool IsNewUser { get; }
    /// <summary>
    /// Gets a value indicating whether the workspace is new.
    /// </summary>
    public bool IsNewWorkspace { get; }
    /// <summary>
    /// Gets the UTC occurrence time.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; }
}
