using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the supported action codes for a workspace state.
/// </summary>
public interface IWorkspaceActions
{
    /// <summary>
    /// Gets the supported action codes.
    /// </summary>
    /// <param name="state">The current workspace state.</param>
    /// <returns>The supported action codes.</returns>
    IReadOnlyList<string> Codes(WorkspaceState state);
}
