using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the baseline action codes for a workspace state.
/// </summary>
public sealed class WorkspaceActions : IWorkspaceActions
{
    private static readonly string[] list =
    [
        "transaction.expense.add",
        "transaction.income.add",
        "summary.month.show",
        "category.breakdown.show",
        "transaction.recent.show"
    ];
    /// <summary>
    /// Gets the supported action codes for the current state.
    /// </summary>
    /// <param name="state">The current workspace state.</param>
    /// <returns>The supported action codes.</returns>
    public IReadOnlyList<string> Codes(WorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return list;
    }
}
