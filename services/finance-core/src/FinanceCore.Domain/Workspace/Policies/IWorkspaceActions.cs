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
    /// <param name="state">The workspace state code.</param>
    /// <param name="context">The state data required to resolve actions.</param>
    /// <returns>The supported action codes.</returns>
    IReadOnlyList<string> Codes(string state, WorkspaceActionContext context);
}
