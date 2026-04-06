using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers expense workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class ExpenseRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the home state exposes the expense action after one account exists.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Shows the add expense action on home when an account exists")]
    public async Task Shows_expense_action()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-home"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Create(queue, "actor-expense-home", "room-expense-home", "Cash", "USD", "100", "expense-home");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal(["transaction.expense.add", "transaction.income.add", "transaction.recent.show", "summary.month.show", "account.add"], home.Payload.Frame.Actions);
    }
    /// <summary>
    /// Verifies that a single-account expense skips account selection and updates the balance.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an expense and skips account selection when one account exists")]
    public async Task Creates_expense()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-single"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-single", "room-expense-single", "Cash", "USD", "100", "expense-single-account");
        await Publish(Input("actor-expense-single", "room-expense-single", "action", "transaction.expense.add", "expense-single-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "expense-single-1");
        Assert.Equal("transaction.expense.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-expense-single", "room-expense-single", "text", "12.5", "expense-single-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "expense-single-2");
        Assert.Equal("transaction.expense.source", source.Payload.Frame.State);
        await Publish(Input("actor-expense-single", "room-expense-single", "text", "Coffee", "expense-single-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> category = await Take(queue, "expense-single-3");
        Assert.Equal("transaction.expense.category", category.Payload.Frame.State);
        Assert.Contains("transaction.expense.category.1", category.Payload.Frame.Actions, StringComparer.Ordinal);
        await Publish(Input("actor-expense-single", "room-expense-single", "action", "transaction.expense.category.1", "expense-single-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "expense-single-4");
        Assert.Equal("transaction.expense.confirm", confirm.Payload.Frame.State);
        await Publish(Input("actor-expense-single", "room-expense-single", "action", "transaction.expense.create", "expense-single-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "expense-single-5");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Expense was recorded", Notice(home.Payload.Frame.StateData));
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry"));
        Assert.Equal(87.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Equal("Coffee", await Scalar("select coalesce(source_text, '') from finance.transaction_entry"));
        Assert.Equal("coffee", await Scalar("select coalesce(source_key, '') from finance.transaction_entry"));
    }
    /// <summary>
    /// Verifies that a multi-account expense lets the user choose the account before saving.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an expense after choosing one account when multiple accounts exist")]
    public async Task Creates_expense_for_selected_account()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-multi"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-multi", "room-expense-multi", "Cash", "USD", "100", "expense-multi-a");
        await Create(queue, "actor-expense-multi", "room-expense-multi", "Card", "USD", "50", "expense-multi-b");
        await Publish(Input("actor-expense-multi", "room-expense-multi", "action", "transaction.expense.add", "expense-multi-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> account = await Take(queue, "expense-multi-1");
        string card = Account(account.Payload.Frame.StateData, "Card");
        Assert.Equal("transaction.expense.account", account.Payload.Frame.State);
        Assert.Contains(card, account.Payload.Frame.Actions, StringComparer.Ordinal);
        await Publish(Input("actor-expense-multi", "room-expense-multi", "action", card, "expense-multi-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "expense-multi-2");
        Assert.Equal("transaction.expense.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-expense-multi", "room-expense-multi", "text", "7.5", "expense-multi-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "expense-multi-3");
        Assert.Equal("transaction.expense.source", source.Payload.Frame.State);
        await Publish(Input("actor-expense-multi", "room-expense-multi", "text", "Morning coffee", "expense-multi-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> category = await Take(queue, "expense-multi-4");
        Assert.Equal("transaction.expense.category", category.Payload.Frame.State);
        await Publish(Input("actor-expense-multi", "room-expense-multi", "text", "Coffee", "expense-multi-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "expense-multi-5");
        Assert.Equal("transaction.expense.confirm", confirm.Payload.Frame.State);
        await Publish(Input("actor-expense-multi", "room-expense-multi", "action", "transaction.expense.create", "expense-multi-6"));
        _ = await Take(queue, "expense-multi-6");
        Assert.Equal(100m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Equal(42.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Card'"), CultureInfo.InvariantCulture));
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry where account_id = (select id from finance.account where name = 'Card')"));
    }
    /// <summary>
    /// Verifies that cancel returns home without writing a transaction.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Cancels an expense flow without persisting a transaction")]
    public async Task Cancels_expense()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-cancel"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-cancel", "room-expense-cancel", "Cash", "USD", "100", "expense-cancel-account");
        await Publish(Input("actor-expense-cancel", "room-expense-cancel", "action", "transaction.expense.add", "expense-cancel-1"));
        _ = await Take(queue, "expense-cancel-1");
        await Publish(Input("actor-expense-cancel", "room-expense-cancel", "action", "transaction.expense.cancel", "expense-cancel-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "expense-cancel-2");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Expense creation was cancelled", Notice(home.Payload.Frame.StateData));
        Assert.Equal(0, await Number("select count(*) from finance.transaction_entry"));
    }
    /// <summary>
    /// Verifies that invalid and non-positive expense amounts return different validation errors.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns distinct validation errors for invalid, precise, and non-positive expense amounts")]
    public async Task Rejects_expense_amount()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-amount"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-amount", "room-expense-amount", "Cash", "USD", "100", "expense-amount-account");
        await Publish(Input("actor-expense-amount", "room-expense-amount", "action", "transaction.expense.add", "expense-amount-1"));
        _ = await Take(queue, "expense-amount-1");
        await Publish(Input("actor-expense-amount", "room-expense-amount", "text", "abc", "expense-amount-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> invalid = await Take(queue, "expense-amount-2");
        Assert.Equal("transaction.expense.amount", invalid.Payload.Frame.State);
        Assert.Equal("Enter a valid numeric amount", Error(invalid.Payload.Frame.StateData));
        await Publish(Input("actor-expense-amount", "room-expense-amount", "text", "1.23456", "expense-amount-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> precise = await Take(queue, "expense-amount-3");
        Assert.Equal("transaction.expense.amount", precise.Payload.Frame.State);
        Assert.Equal("Enter up to 4 decimal places", Error(precise.Payload.Frame.StateData));
        await Publish(Input("actor-expense-amount", "room-expense-amount", "text", "0", "expense-amount-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> zero = await Take(queue, "expense-amount-4");
        Assert.Equal("transaction.expense.amount", zero.Payload.Frame.State);
        Assert.Equal("Amount must be greater than zero", Error(zero.Payload.Frame.StateData));
    }
    /// <summary>
    /// Verifies that one idempotency key applies the expense exactly once.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Processes the same expense confirmation idempotency key once")]
    public async Task Deduplicates_expense()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-dedupe", "room-expense-dedupe", "Cash", "USD", "100", "expense-dedupe-account");
        await Publish(Input("actor-expense-dedupe", "room-expense-dedupe", "action", "transaction.expense.add", "expense-dedupe-1"));
        _ = await Take(queue, "expense-dedupe-1");
        await Publish(Input("actor-expense-dedupe", "room-expense-dedupe", "text", "10", "expense-dedupe-2"));
        _ = await Take(queue, "expense-dedupe-2");
        await Publish(Input("actor-expense-dedupe", "room-expense-dedupe", "text", "Lunch", "expense-dedupe-3"));
        _ = await Take(queue, "expense-dedupe-3");
        await Publish(Input("actor-expense-dedupe", "room-expense-dedupe", "action", "transaction.expense.category.1", "expense-dedupe-4"));
        _ = await Take(queue, "expense-dedupe-4");
        MessageEnvelope<WorkspaceInputRequestedCommand> item = Input("actor-expense-dedupe", "room-expense-dedupe", "action", "transaction.expense.create", "expense-dedupe-5");
        await Publish(item);
        await Publish(item);
        _ = await Take(queue, "expense-dedupe-5");
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry"));
        Assert.Equal(90m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Null(await View(queue, TimeSpan.FromSeconds(1)));
    }
    /// <summary>
    /// Verifies that repeated free text category names reuse the same user category.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Reuses one user category for repeated free text expense input")]
    public async Task Reuses_category()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-category"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-category", "room-expense-category", "Cash", "USD", "100", "expense-category-account");
        await Record(queue, "actor-expense-category", "room-expense-category", "15", "Morning coffee", "Coffee", "expense-category-one");
        await Record(queue, "actor-expense-category", "room-expense-category", "5", "Afternoon coffee", "coffee", "expense-category-two");
        Assert.Equal(1, await Number("select count(*) from finance.category where scope = 'user' and user_id = (select id from finance.user_account where actor_key = 'actor-expense-category')"));
        Assert.Equal(2, await Number("select count(*) from finance.transaction_entry"));
    }
    /// <summary>
    /// Verifies that a legacy home snapshot without account ids still records an expense.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an expense from a legacy home snapshot without account ids")]
    public async Task Creates_expense_from_legacy_home()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-legacy"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-legacy", "room-expense-legacy", "Cash", "USD", "100", "expense-legacy-account");
        await Execute("update finance.workspace set state_data = '{\"accounts\":[{\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":100}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}'::jsonb where conversation_key = 'room-expense-legacy'");
        await Publish(Input("actor-expense-legacy", "room-expense-legacy", "action", "transaction.expense.add", "expense-legacy-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "expense-legacy-1");
        Assert.Equal("transaction.expense.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-expense-legacy", "room-expense-legacy", "text", "11", "expense-legacy-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "expense-legacy-2");
        Assert.Equal("transaction.expense.source", source.Payload.Frame.State);
        await Publish(Input("actor-expense-legacy", "room-expense-legacy", "text", "Taxi", "expense-legacy-3"));
        _ = await Take(queue, "expense-legacy-3");
        await Publish(Input("actor-expense-legacy", "room-expense-legacy", "action", "transaction.expense.category.1", "expense-legacy-4"));
        _ = await Take(queue, "expense-legacy-4");
        await Publish(Input("actor-expense-legacy", "room-expense-legacy", "action", "transaction.expense.create", "expense-legacy-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "expense-legacy-5");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry"));
        Assert.Equal(89m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that a confirmed expense creates a rule and the next matching source skips category selection.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Learns one expense category rule from confirmed source text")]
    public async Task Learns_expense_rule()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-expense-rule"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-expense-rule", "room-expense-rule", "Cash", "USD", "100", "expense-rule-account");
        await Record(queue, "actor-expense-rule", "room-expense-rule", "15", "Coffee Shop", "Coffee", "expense-rule-one");
        Assert.Equal(1, await Number("select count(*) from finance.category_rule where user_id = (select id from finance.user_account where actor_key = 'actor-expense-rule') and kind = 'expense'"));
        Assert.Equal("Coffee Shop", await Scalar("select source_text from finance.category_rule where user_id = (select id from finance.user_account where actor_key = 'actor-expense-rule') and kind = 'expense'"));
        Assert.Equal("coffee shop", await Scalar("select source_key from finance.category_rule where user_id = (select id from finance.user_account where actor_key = 'actor-expense-rule') and kind = 'expense'"));
        await Publish(Input("actor-expense-rule", "room-expense-rule", "action", "transaction.expense.add", "expense-rule-two-1"));
        _ = await Take(queue, "expense-rule-two-1");
        await Publish(Input("actor-expense-rule", "room-expense-rule", "text", "5", "expense-rule-two-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "expense-rule-two-2");
        Assert.Equal("transaction.expense.source", source.Payload.Frame.State);
        await Publish(Input("actor-expense-rule", "room-expense-rule", "text", "  coffee   shop  ", "expense-rule-two-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "expense-rule-two-3");
        Assert.Equal("transaction.expense.confirm", confirm.Payload.Frame.State);
        Assert.Equal("Category was selected automatically", Notice(confirm.Payload.Frame.StateData));
        Assert.Equal("Coffee", Category(confirm.Payload.Frame.StateData));
        await Publish(Input("actor-expense-rule", "room-expense-rule", "action", "transaction.expense.create", "expense-rule-two-4"));
        _ = await Take(queue, "expense-rule-two-4");
        Assert.Equal(2, await Number("select count(*) from finance.transaction_entry where user_id = (select id from finance.user_account where actor_key = 'actor-expense-rule') and kind = 'expense'"));
        Assert.Equal(1, await Number("select count(distinct source_key) from finance.transaction_entry where user_id = (select id from finance.user_account where actor_key = 'actor-expense-rule') and kind = 'expense'"));
    }
    private async Task Record(string queue, string actor, string room, string amount, string source, string category, string id)
    {
        await Publish(Input(actor, room, "action", "transaction.expense.add", $"{id}-1"));
        _ = await Take(queue, $"{id}-1");
        await Publish(Input(actor, room, "text", amount, $"{id}-2"));
        _ = await Take(queue, $"{id}-2");
        await Publish(Input(actor, room, "text", source, $"{id}-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Take(queue, $"{id}-3");
        if (string.Equals(view.Payload.Frame.State, "transaction.expense.category", StringComparison.Ordinal))
        {
            await Publish(Input(actor, room, "text", category, $"{id}-4"));
            _ = await Take(queue, $"{id}-4");
            await Publish(Input(actor, room, "action", "transaction.expense.create", $"{id}-5"));
            _ = await Take(queue, $"{id}-5");
            return;
        }
        if (!string.Equals(view.Payload.Frame.State, "transaction.expense.confirm", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected workspace state '{view.Payload.Frame.State}'");
        }
        await Publish(Input(actor, room, "action", "transaction.expense.create", $"{id}-4"));
        _ = await Take(queue, $"{id}-4");
    }
    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Create(string queue, string actor, string room, string name, string currency, string balance, string id)
    {
        await Publish(Envelope(actor, room, string.Empty, $"workspace-requested-{id}"));
        _ = await Take(queue, $"workspace-requested-{id}");
        await Publish(Input(actor, room, "action", "account.add", $"workspace-input-{id}-1"));
        _ = await Take(queue, $"workspace-input-{id}-1");
        await Publish(Input(actor, room, "text", name, $"workspace-input-{id}-2"));
        _ = await Take(queue, $"workspace-input-{id}-2");
        await Publish(Currency(actor, room, currency, $"workspace-input-{id}-3"));
        _ = await Take(queue, $"workspace-input-{id}-3");
        await Publish(Input(actor, room, "text", balance, $"workspace-input-{id}-4"));
        _ = await Take(queue, $"workspace-input-{id}-4");
        await Publish(Input(actor, room, "action", "account.create", $"workspace-input-{id}-5"));
        return await Take(queue, $"workspace-input-{id}-5");
    }
    private static MessageEnvelope<WorkspaceInputRequestedCommand> Currency(string actor, string room, string currency, string id)
    {
        string text = currency.Trim().ToUpperInvariant();
        return text switch
        {
            "RUB" => Input(actor, room, "action", "account.currency.rub", id),
            "USD" => Input(actor, room, "action", "account.currency.usd", id),
            "EUR" => Input(actor, room, "action", "account.currency.eur", id),
            _ => Input(actor, room, "text", currency, id)
        };
    }
    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Take(string queue, string step)
    {
        MessageEnvelope<WorkspaceViewRequestedCommand>? item = await View(queue);
        return item ?? throw new InvalidOperationException($"Workspace view is missing after '{step}'");
    }
    private static string Notice(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("status").GetProperty("notice").GetString() ?? string.Empty;
    }
    private static string Error(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("status").GetProperty("error").GetString() ?? string.Empty;
    }
    private static string Category(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("expense").GetProperty("category").GetProperty("name").GetString() ?? string.Empty;
    }
    private static string Account(string data, string name)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("choices").GetProperty("accounts").EnumerateArray())
        {
            if (string.Equals(node.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                return $"transaction.expense.account.{node.GetProperty("slot").GetInt32()}";
            }
        }
        throw new InvalidOperationException($"Workspace state is missing account choice '{name}'");
    }
}
