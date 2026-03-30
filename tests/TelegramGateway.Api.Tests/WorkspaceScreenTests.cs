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
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.add"]), new WorkspaceViewFreshness(true, true), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Equal("sendMessage", data.Method);
        Assert.Equal("HTML", data.ParseMode);
        Assert.Contains("<b>Finance workspace</b>", data.Text, StringComparison.Ordinal);
        Assert.Single(data.Keys.SelectMany(item => item.Cells));
        Assert.Equal("➕ Add account", data.Keys.SelectMany(item => item.Cells).Single().Text);
    }
    /// <summary>
    /// Verifies that the home screen includes transaction actions when accounts exist.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen with transaction actions")]
    public void Builds_home_screen_with_expense()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"recent\":{\"page\":0,\"hasPrevious\":false,\"hasNext\":false,\"items\":[],\"selected\":{\"id\":\"\",\"kind\":\"\",\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":0,\"currency\":\"\",\"occurredUtc\":\"0001-01-01T00:00:00+00:00\"}},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.add", "transaction.income.add", "transaction.recent.show", "account.add"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Your accounts</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["➖ Add expense", "➕ Add income", "🧾 Recent transactions", "➕ Add account"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the confirmation screen includes the account draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the confirm workspace screen for Telegram delivery")]
    public void Builds_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"RUB\",\"amount\":1200},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Confirm account</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Balance: <b>1 200 ₽ (<code>RUB</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(2, data.Keys.SelectMany(item => item.Cells).Count());
    }
    /// <summary>
    /// Verifies that the expense account screen renders dynamic account buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the expense account screen for Telegram delivery")]
    public void Builds_expense_account_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.account", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200},{\"id\":\"a2\",\"name\":\"Card\",\"currency\":\"USD\",\"amount\":800}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"choices\":{\"accounts\":[{\"slot\":1,\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},{\"slot\":2,\"id\":\"a2\",\"name\":\"Card\",\"note\":\"USD\"}],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.account.1", "transaction.expense.account.2", "transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>New expense</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["Cash · USD", "Card · USD", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the expense category screen renders dynamic category buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the expense category screen for Telegram delivery")]
    public void Builds_expense_category_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.category", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":12.5},\"choices\":{\"accounts\":[],\"categories\":[{\"slot\":1,\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},{\"slot\":2,\"id\":\"c2\",\"name\":\"Coffee\",\"note\":\"\"}]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.category.1", "transaction.expense.category.2", "transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("Choose the category or send a new name", data.Text, StringComparison.Ordinal);
        Assert.Equal(["🍽 Food", "Coffee", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the expense confirm screen includes the draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the expense confirm screen for Telegram delivery")]
    public void Builds_expense_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},\"amount\":12.5},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.create", "transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Confirm expense</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category: <b>&#127869; Food</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Amount: <b>12.5 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✅ Save expense", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income account screen renders dynamic account buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the income account screen for Telegram delivery")]
    public void Builds_income_account_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.account", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200},{\"id\":\"a2\",\"name\":\"Card\",\"currency\":\"USD\",\"amount\":800}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"choices\":{\"accounts\":[{\"slot\":1,\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},{\"slot\":2,\"id\":\"a2\",\"name\":\"Card\",\"note\":\"USD\"}],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.account.1", "transaction.income.account.2", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>New income</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["Cash · USD", "Card · USD", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income category screen renders dynamic category buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the income category screen for Telegram delivery")]
    public void Builds_income_category_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.category", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":25.5},\"choices\":{\"accounts\":[],\"categories\":[{\"slot\":1,\"id\":\"c1\",\"name\":\"Salary\",\"note\":\"salary\"},{\"slot\":2,\"id\":\"c2\",\"name\":\"Freelance\",\"note\":\"\"}]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.category.1", "transaction.income.category.2", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("Choose the category or send a new name", data.Text, StringComparison.Ordinal);
        Assert.Equal(["💼 Salary", "Freelance", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income confirm screen includes the draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the income confirm screen for Telegram delivery")]
    public void Builds_income_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Salary\",\"note\":\"salary\"},\"amount\":25.5},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.create", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Confirm income</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category: <b>&#128188; Salary</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Amount: <b>25.5 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✅ Save income", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the recent list screen renders item and paging buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the recent transaction list screen for Telegram delivery")]
    public void Builds_recent_list_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.recent.list", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"recent\":{\"page\":0,\"hasPrevious\":false,\"hasNext\":true,\"items\":[{\"slot\":1,\"id\":\"t1\",\"kind\":\"expense\",\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},\"amount\":12.5,\"currency\":\"USD\",\"occurredUtc\":\"2026-03-29T20:28:00+00:00\"}],\"selected\":{\"id\":\"\",\"kind\":\"\",\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":0,\"currency\":\"\",\"occurredUtc\":\"0001-01-01T00:00:00+00:00\"}},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.recent.item.1", "transaction.recent.page.next", "transaction.recent.back"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Recent transactions</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["- 🍽 Food · 12.5 $", "Next ▶", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the recent detail screen renders corrective actions.
    /// </summary>
    [Fact(DisplayName = "Builds the recent transaction detail screen for Telegram delivery")]
    public void Builds_recent_detail_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.recent.detail", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"recent\":{\"page\":0,\"hasPrevious\":false,\"hasNext\":false,\"items\":[],\"selected\":{\"slot\":1,\"id\":\"t1\",\"kind\":\"income\",\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Salary\",\"note\":\"salary\"},\"amount\":25.5,\"currency\":\"USD\",\"occurredUtc\":\"2026-03-29T20:28:00+00:00\"}},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.recent.delete", "transaction.recent.recategorize", "transaction.recent.back"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("<b>Transaction</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["🗑 Delete", "✏ Change category", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that unknown currencies keep the code without a symbol.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen for unknown currency codes")]
    public void Builds_code()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[{\"name\":\"Vault\",\"currency\":\"ABC\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.add"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("- <b>Vault</b>: 1 200 <code>ABC</code>", data.Text, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that user input is escaped for HTML rendering.
    /// </summary>
    [Fact(DisplayName = "Escapes HTML sensitive account names in the confirm screen")]
    public void Escapes_name()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"<cash&card>\",\"currency\":\"USD\",\"amount\":1200},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = WorkspaceScreen.Message(100, note);
        Assert.Contains("&lt;cash&amp;card&gt;", data.Text, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing state data is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing state data for confirm screen")]
    public void Rejects_state_data()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", string.Empty, ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => WorkspaceScreen.Message(100, note));
        Assert.Contains("StateData", error.Message, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing currency is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing currency for balance screen")]
    public void Rejects_currency()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.balance", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => WorkspaceScreen.Message(100, note));
        Assert.Contains("requires currency", error.Message, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing amount is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing amount for confirm screen")]
    public void Rejects_amount()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => WorkspaceScreen.Message(100, note));
        Assert.Contains("requires amount", error.Message, StringComparison.Ordinal);
    }
}
