using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Entry.Workspace.Slices;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace view rendering for Telegram delivery.
/// </summary>
public sealed class WorkspaceScreenTests
{
    /// <summary>
    /// Verifies that the home screen message contains actions and buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen for Telegram delivery")]
    public void Builds_home_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "home", ["transaction.expense.add", "summary.month.show"], true, true, DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Equal("sendMessage", data.Method);
        Assert.Contains("Welcome to your finance workspace", data.Text, StringComparison.Ordinal);
        Assert.Equal(2, data.Keys.SelectMany(item => item.Cells).Count());
    }
}
