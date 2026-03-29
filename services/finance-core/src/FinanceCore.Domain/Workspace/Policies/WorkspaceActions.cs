using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the baseline action codes for a workspace state.
/// </summary>
public sealed class WorkspaceActions : IWorkspaceActions
{
    private const string Cancel = "account.cancel";
    private const string ExpenseCancel = "transaction.expense.cancel";
    /// <summary>
    /// Gets the supported action codes for the workspace state.
    /// </summary>
    /// <param name="state">The workspace state code.</param>
    /// <param name="context">The state data required to resolve actions.</param>
    /// <returns>The supported action codes.</returns>
    public IReadOnlyList<string> Codes(string state, WorkspaceActionContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(context);
        return state switch
        {
            "home" when context.HomeAccountCount > 0 => ["transaction.expense.add", "account.add"],
            "home" => ["account.add"],
            "account.name" => [Cancel],
            "account.currency" when context.Custom => [Cancel],
            "account.currency" => ["account.currency.rub", "account.currency.usd", "account.currency.eur", "account.currency.other", Cancel],
            "account.balance" => [Cancel],
            "account.confirm" => ["account.create", Cancel],
            "transaction.expense.account" => [.. Enumerable.Range(1, context.AccountChoiceCount).Select(item => $"transaction.expense.account.{item}"), ExpenseCancel],
            "transaction.expense.amount" => [ExpenseCancel],
            "transaction.expense.category" => [.. Enumerable.Range(1, context.CategoryChoiceCount).Select(item => $"transaction.expense.category.{item}"), ExpenseCancel],
            "transaction.expense.confirm" => ["transaction.expense.create", ExpenseCancel],
            _ => throw new InvalidOperationException($"WorkspaceActions.Codes does not support state '{state}' and cannot fall back to {Cancel}")
        };
    }
}
