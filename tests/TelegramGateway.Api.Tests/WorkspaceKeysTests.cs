using TelegramGateway.Application.Entry.Workspace.Slices;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace Telegram button composition.
/// </summary>
public sealed class WorkspaceKeysTests
{
    /// <summary>
    /// Verifies that recent state data becomes a Telegram button label.
    /// </summary>
    [Fact(DisplayName = "Builds dynamic recent transaction buttons from state data")]
    public void Builds_recent_buttons()
    {
        var html = new WorkspaceHtml();
        var keys = new WorkspaceKeys(html);
        WorkspaceData data = WorkspaceStateNote.RecentList(0, false, false, [WorkspaceStateNote.RecentItem(1, "t1", "expense", "Food", "food", 12.5m, new DateTimeOffset(2026, 3, 29, 20, 28, 0, TimeSpan.Zero))]);
        IReadOnlyList<TelegramGateway.Application.Telegram.Delivery.TelegramRow> rows = keys.Rows(["transaction.recent.item.1"], data);
        Assert.Equal("1. - 🍽 Food · 12.5 $", rows.SelectMany(item => item.Cells).Single().Text);
    }

    /// <summary>
    /// Verifies that summary action codes map to month navigation buttons.
    /// </summary>
    [Fact(DisplayName = "Builds monthly summary navigation buttons")]
    public void Builds_summary_buttons()
    {
        WorkspaceHtml html = new();
        WorkspaceKeys keys = new(html);
        WorkspaceData data = WorkspaceStateNote.Summary(2026, 4, []);
        IReadOnlyList<TelegramGateway.Application.Telegram.Delivery.TelegramRow> rows = keys.Rows(["summary.month.show", "category.month.show", "summary.month.prev", "summary.month.next", "summary.month.back"], data);
        Assert.Equal(["📊 Monthly summary", "🗂 Category breakdown", "◀ Previous month", "Next month ▶", "↩ Back"], rows.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }

    /// <summary>
    /// Verifies that breakdown action codes map to month navigation buttons.
    /// </summary>
    [Fact(DisplayName = "Builds category breakdown navigation buttons")]
    public void Builds_breakdown_buttons()
    {
        WorkspaceHtml html = new();
        WorkspaceKeys keys = new(html);
        WorkspaceData data = WorkspaceStateNote.Breakdown(2026, 4, []);
        IReadOnlyList<TelegramGateway.Application.Telegram.Delivery.TelegramRow> rows = keys.Rows(["category.month.prev", "category.month.next", "category.month.back"], data);
        Assert.Equal(["◀ Previous month", "Next month ▶", "↩ Back"], rows.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }

    /// <summary>
    /// Verifies that unknown actions fail fast.
    /// </summary>
    [Fact(DisplayName = "Rejects unknown workspace action codes")]
    public void Rejects_unknown_codes()
    {
        WorkspaceHtml html = new();
        WorkspaceKeys keys = new(html);
        WorkspaceData data = new()
        {
            Accounts = [],
            Financial = new FinancialData(),
            Expense = new TransactionData(),
            Income = new TransactionData(),
            Recent = new RecentData(),
            Choices = new ChoicesData(),
            Status = new StatusData(),
            Custom = false
        };
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => keys.Rows(["workspace.unknown"], data));
        Assert.Contains("is not recognized", error.Message, StringComparison.Ordinal);
    }
}
