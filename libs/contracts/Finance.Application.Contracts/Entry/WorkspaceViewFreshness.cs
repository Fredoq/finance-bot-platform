namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the novelty flags of the rendered workspace.
/// </summary>
public sealed record WorkspaceViewFreshness
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="isNewUser">Indicates whether the user was created by the workflow.</param>
    /// <param name="isNewWorkspace">Indicates whether the workspace was created by the workflow.</param>
    public WorkspaceViewFreshness(bool isNewUser, bool isNewWorkspace)
    {
        IsNewUser = isNewUser;
        IsNewWorkspace = isNewWorkspace;
    }
    /// <summary>
    /// Gets a value indicating whether the user was created by the workflow.
    /// </summary>
    public bool IsNewUser { get; }
    /// <summary>
    /// Gets a value indicating whether the workspace was created by the workflow.
    /// </summary>
    public bool IsNewWorkspace { get; }
}
