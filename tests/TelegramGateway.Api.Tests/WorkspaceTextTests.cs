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
        string data = text.Text("transaction.recent.detail", false, WorkspaceStateNote.RecentDetail(WorkspaceStateNote.RecentItem(1, "t1", "income", "Salary", "salary", 25.5m, new DateTimeOffset(2026, 3, 29, 20, 28, 0, TimeSpan.Zero))));
        Assert.Contains("<b>Transaction</b>", data, StringComparison.Ordinal);
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
}
