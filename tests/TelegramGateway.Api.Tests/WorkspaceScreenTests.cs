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
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[]}", ["account.add"]), new WorkspaceViewFreshness(true, true), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Equal("sendMessage", data.Method);
        Assert.Equal("HTML", data.ParseMode);
        Assert.Contains("<b>Finance workspace</b>", data.Text, StringComparison.Ordinal);
        Assert.Single(data.Keys.SelectMany(item => item.Cells));
        Assert.Equal("➕ Add account", data.Keys.SelectMany(item => item.Cells).Single().Text);
    }
    /// <summary>
    /// Verifies that the confirmation screen includes the account draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the confirm workspace screen for Telegram delivery")]
    public void Builds_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"name\":\"Cash\",\"currency\":\"RUB\",\"amount\":1200}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Confirm account</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Balance: <b>1 200 ₽ (<code>RUB</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(2, data.Keys.SelectMany(item => item.Cells).Count());
    }
    /// <summary>
    /// Verifies that unknown currencies keep the code without a symbol.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen for unknown currency codes")]
    public void Builds_code()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[{\"name\":\"Vault\",\"currency\":\"ABC\",\"amount\":1200}]}", ["account.add"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("- <b>Vault</b>: 1 200 <code>ABC</code>", data.Text, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that user input is escaped for HTML rendering.
    /// </summary>
    [Fact(DisplayName = "Escapes HTML sensitive account names in the confirm screen")]
    public void Escapes_name()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"name\":\"<cash&card>\",\"currency\":\"USD\",\"amount\":1200}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("&lt;cash&amp;card&gt;", data.Text, StringComparison.Ordinal);
    }
}
