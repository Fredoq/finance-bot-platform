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
}
