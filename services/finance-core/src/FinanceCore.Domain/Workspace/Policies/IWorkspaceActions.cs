namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the supported action codes for a workspace state.
/// </summary>
public interface IWorkspaceActions
{
    /// <summary>
    /// Gets the supported action codes.
    /// </summary>
    /// <returns>The supported action codes.</returns>
    IReadOnlyList<string> Codes();
}
