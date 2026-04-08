using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Entry.Workspace.Slices;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace view rendering for Telegram delivery.
/// </summary>
public sealed class WorkspaceScreenTests
{
    private static readonly ITelegramKeys keys = new TelegramKeys();
    private readonly WorkspaceScreen screen = Build();
    /// <summary>
    /// Verifies that the home screen message contains actions and buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen for Telegram delivery")]
    public void Builds_home_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.add", "profile.timezone.show"]), new WorkspaceViewFreshness(true, true), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Equal("sendMessage", data.Method);
        Assert.Equal("HTML", data.ParseMode);
        Assert.Contains("<b>Finance workspace</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["➕ Add account", "🕒 Time zone"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the home screen includes transaction actions when accounts exist.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen with transaction actions")]
    public void Builds_home_screen_with_expense()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"recent\":{\"page\":0,\"hasPrevious\":false,\"hasNext\":false,\"items\":[],\"selected\":{\"id\":\"\",\"kind\":\"\",\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":0,\"currency\":\"\",\"occurredUtc\":\"0001-01-01T00:00:00+00:00\"}},\"summary\":{\"year\":0,\"month\":0,\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.add", "transaction.income.add", "transaction.recent.show", "summary.month.show", "profile.timezone.show", "account.add"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Your accounts</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["➖ Add expense", "➕ Add income", "🧾 Recent transactions", "📊 Monthly summary", "🕒 Time zone", "➕ Add account"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the confirmation screen includes the account draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the confirm workspace screen for Telegram delivery")]
    public void Builds_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"RUB\",\"amount\":1200},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
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
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>New expense</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["Cash · USD", "Card · USD", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the expense category screen renders dynamic category buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the expense category screen for Telegram delivery")]
    public void Builds_expense_category_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.category", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":12.5,\"source\":\"Morning coffee\"},\"choices\":{\"accounts\":[],\"categories\":[{\"slot\":1,\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},{\"slot\":2,\"id\":\"c2\",\"name\":\"Coffee\",\"note\":\"\"}]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.category.1", "transaction.expense.category.2", "transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("Source: <b>Morning coffee</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Choose the category or send a new name", data.Text, StringComparison.Ordinal);
        Assert.Equal(["🍽 Food", "Coffee", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the expense confirm screen includes the draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the expense confirm screen for Telegram delivery")]
    public void Builds_expense_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},\"amount\":12.5,\"source\":\"Morning coffee\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"Category was selected automatically\"},\"custom\":false}", ["transaction.expense.create", "transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Confirm expense</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category was selected automatically", data.Text, StringComparison.Ordinal);
        Assert.Contains("Source: <b>Morning coffee</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category: <b>&#127869; Food</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Amount: <b>12.5 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✅ Save expense", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the expense source screen prompts for merchant text.
    /// </summary>
    [Fact(DisplayName = "Builds the expense source screen for Telegram delivery")]
    public void Builds_expense_source_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.expense.source", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":12.5,\"source\":\"\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.expense.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("Send the merchant or description", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income account screen renders dynamic account buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the income account screen for Telegram delivery")]
    public void Builds_income_account_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.account", "{\"accounts\":[{\"id\":\"a1\",\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":1200},{\"id\":\"a2\",\"name\":\"Card\",\"currency\":\"USD\",\"amount\":800}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"income\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null},\"choices\":{\"accounts\":[{\"slot\":1,\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},{\"slot\":2,\"id\":\"a2\",\"name\":\"Card\",\"note\":\"USD\"}],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.account.1", "transaction.income.account.2", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>New income</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["Cash · USD", "Card · USD", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income category screen renders dynamic category buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the income category screen for Telegram delivery")]
    public void Builds_income_category_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.category", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":25.5,\"source\":\"Client payment\"},\"choices\":{\"accounts\":[],\"categories\":[{\"slot\":1,\"id\":\"c1\",\"name\":\"Salary\",\"note\":\"salary\"},{\"slot\":2,\"id\":\"c2\",\"name\":\"Freelance\",\"note\":\"\"}]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.category.1", "transaction.income.category.2", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("Source: <b>Client payment</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Choose the category or send a new name", data.Text, StringComparison.Ordinal);
        Assert.Equal(["💼 Salary", "Freelance", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income confirm screen includes the draft summary.
    /// </summary>
    [Fact(DisplayName = "Builds the income confirm screen for Telegram delivery")]
    public void Builds_income_confirm_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Salary\",\"note\":\"salary\"},\"amount\":25.5,\"source\":\"Client payment\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"Category was selected automatically\"},\"custom\":false}", ["transaction.income.create", "transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Confirm income</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category was selected automatically", data.Text, StringComparison.Ordinal);
        Assert.Contains("Source: <b>Client payment</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Category: <b>&#128188; Salary</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Amount: <b>25.5 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✅ Save income", "✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the income source screen prompts for merchant text.
    /// </summary>
    [Fact(DisplayName = "Builds the income source screen for Telegram delivery")]
    public void Builds_income_source_screen()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("transaction.income.source", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":25.5,\"source\":\"\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["transaction.income.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("Send the merchant or description", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the recent list screen renders item and paging buttons.
    /// </summary>
    [Fact(DisplayName = "Builds the recent transaction list screen for Telegram delivery")]
    public void Builds_recent_list_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("transaction.recent.list", WorkspaceStateNote.RecentList(0, false, true, [WorkspaceStateNote.RecentItem(1, "t1", "expense", "Food", "food", 12.5m, new DateTimeOffset(2026, 3, 29, 20, 28, 0, TimeSpan.Zero))]), "transaction.recent.item.1", "transaction.recent.page.next", "transaction.recent.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Recent transactions</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["1. - 🍽 Food · 12.5 $", "Next ▶", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that an empty recent list is rendered without requiring a notice.
    /// </summary>
    [Fact(DisplayName = "Builds the empty recent transaction list screen for Telegram delivery")]
    public void Builds_empty_recent_list_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("transaction.recent.list", WorkspaceStateNote.RecentList(0, false, false, []), "transaction.recent.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("No transactions yet", data.Text, StringComparison.Ordinal);
        Assert.Equal(["↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that the recent detail screen renders corrective actions.
    /// </summary>
    [Fact(DisplayName = "Builds the recent transaction detail screen for Telegram delivery")]
    public void Builds_recent_detail_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("transaction.recent.detail", WorkspaceStateNote.RecentDetail(WorkspaceStateNote.RecentItem(1, "t1", "income", "Salary", "salary", 25.5m, new DateTimeOffset(2026, 3, 29, 20, 28, 0, TimeSpan.Zero)) with { Source = "Client payment" }), "transaction.recent.delete", "transaction.recent.recategorize", "transaction.recent.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Transaction</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Source: <b>Client payment</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["🗑 Delete", "✏ Change category", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }

    /// <summary>
    /// Verifies that the summary screen renders totals and month navigation.
    /// </summary>
    [Fact(DisplayName = "Builds the monthly summary screen for Telegram delivery")]
    public void Builds_summary_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("summary.month", WorkspaceStateNote.Summary(2026, 4, [WorkspaceStateNote.Currency("USD", 100m, 40m, WorkspaceStateNote.Account("a2", "Card", 0m, 40m), WorkspaceStateNote.Account("a1", "Cash", 100m, 0m))]), "category.month.show", "summary.month.prev", "summary.month.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Monthly summary</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("April 2026", data.Text, StringComparison.Ordinal);
        Assert.Contains("Income: <b>100 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["🗂 Category breakdown", "◀ Previous month", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }

    /// <summary>
    /// Verifies that the empty summary month renders the fallback text.
    /// </summary>
    [Fact(DisplayName = "Builds the empty monthly summary screen for Telegram delivery")]
    public void Builds_empty_summary_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("summary.month", WorkspaceStateNote.Summary(2026, 4, []), "summary.month.prev", "summary.month.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("No transactions in this month", data.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the breakdown screen renders totals and month navigation.
    /// </summary>
    [Fact(DisplayName = "Builds the category breakdown screen for Telegram delivery")]
    public void Builds_breakdown_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("category.month", WorkspaceStateNote.Breakdown(2026, 4, [WorkspaceStateNote.BreakdownCurrency("USD", 40m, WorkspaceStateNote.BreakdownCategory("Food", "food", 30m, 0.75m), WorkspaceStateNote.BreakdownCategory("Travel", "travel", 10m, 0.25m))]), "category.month.prev", "category.month.back");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Category breakdown</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Expense total: <b>40 $ (<code>USD</code>)</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("75%", data.Text, StringComparison.Ordinal);
        Assert.Equal(["◀ Previous month", "↩ Back"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that unknown currencies keep the code without a symbol.
    /// </summary>
    [Fact(DisplayName = "Builds the home workspace screen for unknown currency codes")]
    public void Builds_code()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("home", "{\"accounts\":[{\"name\":\"Vault\",\"currency\":\"ABC\",\"amount\":1200}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.add", "profile.timezone.show"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("- <b>Vault</b>: 1 200 <code>ABC</code>", data.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the time zone edit screen renders the current value and cancel action.
    /// </summary>
    [Fact(DisplayName = "Builds the time zone workspace screen for Telegram delivery")]
    public void Builds_time_zone_screen()
    {
        WorkspaceViewRequestedCommand note = WorkspaceStateNote.View("profile.timezone.edit", WorkspaceStateNote.TimeZone("Europe/Moscow"), "profile.timezone.cancel");
        TelegramText data = screen.Message(100, note);
        Assert.Contains("<b>Time zone</b>", data.Text, StringComparison.Ordinal);
        Assert.Contains("Current: <code>Europe/Moscow</code>", data.Text, StringComparison.Ordinal);
        Assert.Equal(["✖ Cancel"], data.Keys.SelectMany(item => item.Cells).Select(item => item.Text).ToArray());
    }
    /// <summary>
    /// Verifies that user input is escaped for HTML rendering.
    /// </summary>
    [Fact(DisplayName = "Escapes HTML sensitive account names in the confirm screen")]
    public void Escapes_name()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"<cash&card>\",\"currency\":\"USD\",\"amount\":1200},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        TelegramText data = screen.Message(100, note);
        Assert.Contains("&lt;cash&amp;card&gt;", data.Text, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing state data is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing state data for confirm screen")]
    public void Rejects_state_data()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", string.Empty, ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => screen.Message(100, note));
        Assert.Contains("StateData", error.Message, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing currency is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing currency for balance screen")]
    public void Rejects_currency()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.balance", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => screen.Message(100, note));
        Assert.Contains("requires currency", error.Message, StringComparison.Ordinal);
    }
    /// <summary>
    /// Verifies that missing amount is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects missing amount for confirm screen")]
    public void Rejects_amount()
    {
        var note = new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}", ["account.create", "account.cancel"]), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => screen.Message(100, note));
        Assert.Contains("requires amount", error.Message, StringComparison.Ordinal);
    }
    private static WorkspaceScreen Build()
    {
        var html = new WorkspaceHtml();
        return new WorkspaceScreen(new WorkspaceBody(), new WorkspaceText(html), new WorkspaceKeys(html), keys);
    }
}
