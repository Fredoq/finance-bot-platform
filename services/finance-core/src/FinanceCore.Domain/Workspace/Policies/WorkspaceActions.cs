using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the baseline action codes for a workspace state.
/// </summary>
public sealed class WorkspaceActions : IWorkspaceActions
{
    private const string Cancel = "account.cancel";
    private const string ExpenseCancel = "transaction.expense.cancel";
    private const string IncomeCancel = "transaction.income.cancel";
    private const string RecentBack = "transaction.recent.back";
    private const string RecentPrevious = "transaction.recent.page.prev";
    private const string RecentNext = "transaction.recent.page.next";
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
            "home" when context.HomeAccountCount > 0 => ["transaction.expense.add", "transaction.income.add", "transaction.recent.show", "account.add"],
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
            "transaction.income.account" => [.. Enumerable.Range(1, context.AccountChoiceCount).Select(item => $"transaction.income.account.{item}"), IncomeCancel],
            "transaction.income.amount" => [IncomeCancel],
            "transaction.income.category" => [.. Enumerable.Range(1, context.CategoryChoiceCount).Select(item => $"transaction.income.category.{item}"), IncomeCancel],
            "transaction.income.confirm" => ["transaction.income.create", IncomeCancel],
            "transaction.recent.list" => RecentList(context),
            "transaction.recent.detail" => ["transaction.recent.delete", "transaction.recent.recategorize", RecentBack],
            "transaction.recent.delete.confirm" => ["transaction.recent.delete.apply", RecentBack],
            "transaction.recent.category" => [.. Enumerable.Range(1, context.CategoryChoiceCount).Select(item => $"transaction.recent.category.{item}"), RecentBack],
            "transaction.recent.recategorize.confirm" => ["transaction.recent.recategorize.apply", RecentBack],
            _ => throw new InvalidOperationException($"WorkspaceActions.Codes does not support state '{state}' and cannot fall back to {Cancel}")
        };
    }
    private static List<string> RecentList(WorkspaceActionContext context)
    {
        var list = new List<string>(context.RecentItemCount + 3);
        list.AddRange(Enumerable.Range(1, context.RecentItemCount).Select(item => $"transaction.recent.item.{item}"));
        if (context.HasPrevious)
        {
            list.Add(RecentPrevious);
        }
        if (context.HasNext)
        {
            list.Add(RecentNext);
        }
        list.Add(RecentBack);
        return list;
    }
}
