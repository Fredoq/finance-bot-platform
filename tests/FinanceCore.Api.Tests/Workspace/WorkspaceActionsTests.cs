using FinanceCore.Domain.Workspace.Models;
using FinanceCore.Domain.Workspace.Policies;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace action policy behavior.
/// </summary>
public sealed class WorkspaceActionsTests
{
    /// <summary>
    /// Verifies that home state returns the add-account action.
    /// </summary>
    [Fact(DisplayName = "Returns codes for home state")]
    public void Returns_codes_for_home()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> code = item.Codes("home", new WorkspaceActionContext(0, 0, 0, false));
        Assert.Equal(["account.add"], code);
    }
    /// <summary>
    /// Verifies that account currency state returns codes for both custom modes.
    /// </summary>
    [Fact(DisplayName = "Returns codes for account currency state with and without custom mode")]
    public void Returns_codes_for_currency()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> code = item.Codes("account.currency", new WorkspaceActionContext(0, 0, 0, false));
        IReadOnlyList<string> custom = item.Codes("account.currency", new WorkspaceActionContext(0, 0, 0, true));
        Assert.Equal(["account.currency.rub", "account.currency.usd", "account.currency.eur", "account.currency.other", "account.cancel"], code);
        Assert.Equal(["account.cancel"], custom);
    }
    /// <summary>
    /// Verifies that home state includes the expense action when accounts exist.
    /// </summary>
    [Fact(DisplayName = "Returns codes for home state with accounts")]
    public void Returns_codes_for_expense_home()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> code = item.Codes("home", new WorkspaceActionContext(1, 0, 0, false));
        Assert.Equal(["transaction.expense.add", "account.add"], code);
    }
    /// <summary>
    /// Verifies that expense selection states return dynamic slot actions.
    /// </summary>
    [Fact(DisplayName = "Returns dynamic codes for expense selection states")]
    public void Returns_codes_for_expense_states()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> account = item.Codes("transaction.expense.account", new WorkspaceActionContext(2, 2, 0, false));
        IReadOnlyList<string> category = item.Codes("transaction.expense.category", new WorkspaceActionContext(2, 0, 3, false));
        Assert.Equal(["transaction.expense.account.1", "transaction.expense.account.2", "transaction.expense.cancel"], account);
        Assert.Equal(["transaction.expense.category.1", "transaction.expense.category.2", "transaction.expense.category.3", "transaction.expense.cancel"], category);
    }
    /// <summary>
    /// Verifies that empty states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty workspace states")]
    public void Rejects_empty_state()
    {
        var item = new WorkspaceActions();
        Assert.Throws<ArgumentException>(() => item.Codes(string.Empty, new WorkspaceActionContext(0, 0, 0, false)));
    }
    /// <summary>
    /// Verifies that unsupported states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects unsupported workspace states")]
    public void Rejects_unknown_state()
    {
        var item = new WorkspaceActions();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => item.Codes("account.unknown", new WorkspaceActionContext(0, 0, 0, false)));
        Assert.Contains("WorkspaceActions.Codes", error.Message, StringComparison.Ordinal);
    }
}
