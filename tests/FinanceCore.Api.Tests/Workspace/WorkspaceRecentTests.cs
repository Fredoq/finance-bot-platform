using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers recent transaction workspace transitions.
/// </summary>
public sealed class WorkspaceRecentTests
{
    private static readonly DateTimeOffset when = new(2026, 3, 29, 20, 28, 0, TimeSpan.Zero);
    /// <summary>
    /// Verifies that empty category text is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty category text when recategorizing a recent transaction")]
    public void Rejects_empty_category()
    {
        var body = new WorkspaceBody();
        var item = new WorkspaceRecent(body);
        var data = new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData { Recent = new RecentData(0, false, false, [], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))) });
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
        var data = new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData { Recent = new RecentData(0, false, false, [new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))) });
        WorkspaceMove move = item.Return(data, WorkspaceBody.RecentDetailState);
        Assert.Equal(WorkspaceBody.RecentListState, move.Code);
    }

    /// <summary>
    /// Verifies that returning from delete confirmation clears transient status.
    /// </summary>
    [Fact(DisplayName = "Returns from recent delete confirmation with sanitized detail state")]
    public void Returns_delete()
    {
        WorkspaceBody body = new();
        WorkspaceRecent item = new(body);
        WorkspaceData data = new([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData { Recent = new RecentData(0, false, false, [new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))), Choices = new ChoicesData([new OptionData(1, "c2", "Travel", "travel")], []), Status = new StatusData("Use the buttons to confirm or go back", string.Empty) });
        WorkspaceMove move = item.Return(data, WorkspaceBody.RecentDeleteState);
        Assert.Equal(WorkspaceBody.RecentDetailState, move.Code);
        Assert.Equal(string.Empty, move.Body.Status.Error);
        Assert.Equal(string.Empty, move.Body.Status.Notice);
        Assert.Empty(move.Body.Choices.Accounts);
    }

    /// <summary>
    /// Verifies that returning from recategorize confirmation clears transient status.
    /// </summary>
    [Fact(DisplayName = "Returns from recent recategorize confirmation with sanitized detail state")]
    public void Returns_recategorize()
    {
        WorkspaceBody body = new();
        WorkspaceRecent item = new(body);
        WorkspaceData data = new([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData { Recent = new RecentData(0, false, false, [new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 10m, "USD", when))], new RecentItemData(1, new RecentEntryData("t1", "expense", new PickData("a1", "Cash", "USD"), new PickData("c2", "Travel", "travel"), 10m, "USD", when))), Choices = new ChoicesData([], [new OptionData(1, "c2", "Travel", "travel")]), Status = new StatusData("Use the buttons to confirm or go back", string.Empty) });
        WorkspaceMove move = item.Return(data, WorkspaceBody.RecentRecategorizeState);
        Assert.Equal(WorkspaceBody.RecentDetailState, move.Code);
        Assert.Equal(string.Empty, move.Body.Status.Error);
        Assert.Equal(string.Empty, move.Body.Status.Notice);
        Assert.Empty(move.Body.Choices.Categories);
        Assert.Equal("c2", move.Body.Recent.Selected.Category.Id);
    }
}
