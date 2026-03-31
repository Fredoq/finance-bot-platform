using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers recent transaction workspace transitions.
/// </summary>
public sealed class WorkspaceRecentTests
{
    /// <summary>
    /// Verifies that empty category text is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty category text when recategorizing a recent transaction")]
    public void Rejects_empty_category()
    {
        var body = new WorkspaceBody();
        var item = new WorkspaceRecent(body);
        var data = new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(0, false, false, [], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", DateTimeOffset.UtcNow))), new ChoicesData(), new StatusData(), false));
        WorkspaceMove move = item.Text(data, " ");
        Assert.Equal(WorkspaceBody.RecentCategoryState, move.Code);
    }

    /// <summary>
    /// Verifies that returning from recent detail goes back to the list.
    /// </summary>
    [Fact(DisplayName = "Returns from recent detail to the recent list")]
    public void Returns_detail()
    {
        var body = new WorkspaceBody();
        var item = new WorkspaceRecent(body);
        var data = new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(0, false, false, [new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", DateTimeOffset.UtcNow))], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", DateTimeOffset.UtcNow))), new ChoicesData(), new StatusData(), false));
        WorkspaceMove move = item.Return(data, WorkspaceBody.RecentDetailState);
        Assert.Equal(WorkspaceBody.RecentListState, move.Code);
    }
}
