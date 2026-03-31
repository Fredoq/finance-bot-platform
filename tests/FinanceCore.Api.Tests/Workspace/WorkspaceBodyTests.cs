using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace body transformations and account resolution.
/// </summary>
public sealed class WorkspaceBodyTests
{
    /// <summary>
    /// Verifies that the home body keeps the provided notice.
    /// </summary>
    [Fact(DisplayName = "Builds home body with notice")]
    public void Builds_home()
    {
        WorkspaceData data = new WorkspaceBody().Home([], "Tap Add account to start");
        Assert.Equal("Tap Add account to start", data.Status.Notice);
    }

    /// <summary>
    /// Verifies that transaction account resolution falls back to account name and currency.
    /// </summary>
    [Fact(DisplayName = "Resolves account id from matching account name and currency")]
    public void Resolves_account()
    {
        var item = new WorkspaceBody();
        var data = new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData(new FinancialData(), new ExpenseData(new PickData(string.Empty, "Cash", "USD"), new PickData(), 5m), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(), false));
        string value = item.Resolve(data, false);
        Assert.Equal("a1", value);
    }

    /// <summary>
    /// Verifies that updating choices does not clear an in-progress expense draft.
    /// </summary>
    [Fact(DisplayName = "Preserves the expense draft when choices are updated")]
    public void Preserves_expense_draft()
    {
        WorkspaceBody item = new();
        WorkspaceData data = item.Transaction(new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(), false)), new PickData("a1", "Cash", "USD"), new PickData(), 12.5m, false);
        WorkspaceData model = item.Model(data, choices: new ChoicesData([], [new OptionData(1, "c1", "Food", "food")]));
        Assert.Equal("a1", model.Expense.Account.Id);
        Assert.Equal(12.5m, model.Expense.Amount);
    }
}
