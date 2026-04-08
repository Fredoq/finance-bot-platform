using TelegramGateway.Application.Entry.Workspace.Slices;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace text rendering without Telegram transport concerns.
/// </summary>
public sealed class WorkspaceTextTests
{
    /// <summary>
    /// Verifies that recent detail text renders the expected title.
    /// </summary>
    [Fact(DisplayName = "Builds recent transaction detail text with escaped values")]
    public void Builds_recent_detail()
    {
        var text = new WorkspaceText(new WorkspaceHtml());
        string data = text.Text("transaction.recent.detail", false, WorkspaceStateNote.RecentDetail(WorkspaceStateNote.RecentItem(1, "t1", "income", "Salary", "salary", 25.5m, new DateTimeOffset(2026, 3, 29, 20, 28, 0, TimeSpan.Zero)) with { Source = "Client payment" }));
        Assert.Contains("<b>Transaction</b>", data, StringComparison.Ordinal);
        Assert.Contains("Source: <b>Client payment</b>", data, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the empty home state shows onboarding guidance.
    /// </summary>
    [Fact(DisplayName = "Builds home text for the first account onboarding")]
    public void Builds_home()
    {
        var text = new WorkspaceText(new WorkspaceHtml());
        string data = text.Text("home", true, new WorkspaceData());
        Assert.Contains("Add your first account", data, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that summary text renders the month label and totals.
    /// </summary>
    [Fact(DisplayName = "Builds monthly summary text with grouped totals")]
    public void Builds_summary()
    {
        var text = new WorkspaceText(new WorkspaceHtml());
        string data = text.Text("summary.month", false, WorkspaceStateNote.Summary(2026, 4, [WorkspaceStateNote.Currency("USD", 100m, 40m, WorkspaceStateNote.Account("a1", "Cash", 100m, 40m))], timeZone: "Europe/Moscow"));
        Assert.Contains("April 2026", data, StringComparison.Ordinal);
        Assert.Contains("Time zone: <code>Europe/Moscow</code>", data, StringComparison.Ordinal);
        Assert.Contains("Income: <b>100 $ (<code>USD</code>)</b>", data, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that breakdown text renders the month label and category percentages.
    /// </summary>
    [Fact(DisplayName = "Builds category breakdown text with grouped totals")]
    public void Builds_breakdown()
    {
        var text = new WorkspaceText(new WorkspaceHtml());
        string data = text.Text("category.month", false, WorkspaceStateNote.Breakdown(2026, 4, [WorkspaceStateNote.BreakdownCurrency("USD", 40m, WorkspaceStateNote.BreakdownCategory("Food", "food", 30m, 0.75m), WorkspaceStateNote.BreakdownCategory("Travel", "travel", 10m, 0.25m))], timeZone: "Europe/Moscow"));
        Assert.Contains("April 2026", data, StringComparison.Ordinal);
        Assert.Contains("Time zone: <code>Europe/Moscow</code>", data, StringComparison.Ordinal);
        Assert.Contains("Expense total: <b>40 $ (<code>USD</code>)</b>", data, StringComparison.Ordinal);
        Assert.Contains("75%", data, StringComparison.Ordinal);
    }
}
