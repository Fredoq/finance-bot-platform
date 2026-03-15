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
    /// Gets the supported action codes for the workspace baseline.
    /// </summary>
    /// <returns>The supported action codes.</returns>
    public IReadOnlyList<string> Codes() => list;
}
