using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers income workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class IncomeRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the home state exposes the income action after one account exists.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Shows the add income action on home when an account exists")]
    public async Task Shows_income_action()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-home"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Create(queue, "actor-income-home", "room-income-home", "Cash", "USD", "100", "income-home");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal(["transaction.expense.add", "transaction.income.add", "account.add"], home.Payload.Frame.Actions);
    }
    /// <summary>
    /// Verifies that a single-account income skips account selection and updates the balance.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an income and skips account selection when one account exists")]
    public async Task Creates_income()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-single"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-single", "room-income-single", "Cash", "USD", "100", "income-single-account");
        await Publish(Input("actor-income-single", "room-income-single", "action", "transaction.income.add", "income-single-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "income-single-1");
        Assert.Equal("transaction.income.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-income-single", "room-income-single", "text", "12.5", "income-single-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> category = await Take(queue, "income-single-2");
        Assert.Equal("transaction.income.category", category.Payload.Frame.State);
        Assert.Contains("transaction.income.category.1", category.Payload.Frame.Actions, StringComparer.Ordinal);
        await Publish(Input("actor-income-single", "room-income-single", "action", "transaction.income.category.1", "income-single-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "income-single-3");
        Assert.Equal("transaction.income.confirm", confirm.Payload.Frame.State);
        await Publish(Input("actor-income-single", "room-income-single", "action", "transaction.income.create", "income-single-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "income-single-4");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Income was recorded", Notice(home.Payload.Frame.StateData));
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry where kind = 'income'"));
        Assert.Equal(112.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that a multi-account income lets the user choose the account before saving.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an income after choosing one account when multiple accounts exist")]
    public async Task Creates_income_for_selected_account()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-multi"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-multi", "room-income-multi", "Cash", "USD", "100", "income-multi-a");
        await Create(queue, "actor-income-multi", "room-income-multi", "Card", "USD", "50", "income-multi-b");
        await Publish(Input("actor-income-multi", "room-income-multi", "action", "transaction.income.add", "income-multi-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> account = await Take(queue, "income-multi-1");
        string card = Account(account.Payload.Frame.StateData, "Card", "transaction.income.account.");
        Assert.Equal("transaction.income.account", account.Payload.Frame.State);
        Assert.Contains(card, account.Payload.Frame.Actions, StringComparer.Ordinal);
        await Publish(Input("actor-income-multi", "room-income-multi", "action", card, "income-multi-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "income-multi-2");
        Assert.Equal("transaction.income.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-income-multi", "room-income-multi", "text", "7.5", "income-multi-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> category = await Take(queue, "income-multi-3");
        Assert.Equal("transaction.income.category", category.Payload.Frame.State);
        await Publish(Input("actor-income-multi", "room-income-multi", "text", "Freelance", "income-multi-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "income-multi-4");
        Assert.Equal("transaction.income.confirm", confirm.Payload.Frame.State);
        await Publish(Input("actor-income-multi", "room-income-multi", "action", "transaction.income.create", "income-multi-5"));
        _ = await Take(queue, "income-multi-5");
        Assert.Equal(100m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Equal(57.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Card'"), CultureInfo.InvariantCulture));
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry where account_id = (select id from finance.account where name = 'Card') and kind = 'income'"));
    }
    /// <summary>
    /// Verifies that cancel returns home without writing a transaction.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Cancels an income flow without persisting a transaction")]
    public async Task Cancels_income()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-cancel"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-cancel", "room-income-cancel", "Cash", "USD", "100", "income-cancel-account");
        await Publish(Input("actor-income-cancel", "room-income-cancel", "action", "transaction.income.add", "income-cancel-1"));
        _ = await Take(queue, "income-cancel-1");
        await Publish(Input("actor-income-cancel", "room-income-cancel", "action", "transaction.income.cancel", "income-cancel-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "income-cancel-2");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Income creation was cancelled", Notice(home.Payload.Frame.StateData));
        Assert.Equal(0, await Number("select count(*) from finance.transaction_entry"));
    }
    /// <summary>
    /// Verifies that invalid and non-positive income amounts return different validation errors.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns distinct validation errors for invalid, precise, and non-positive income amounts")]
    public async Task Rejects_income_amount()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-amount"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-amount", "room-income-amount", "Cash", "USD", "100", "income-amount-account");
        await Publish(Input("actor-income-amount", "room-income-amount", "action", "transaction.income.add", "income-amount-1"));
        _ = await Take(queue, "income-amount-1");
        await Publish(Input("actor-income-amount", "room-income-amount", "text", "abc", "income-amount-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> invalid = await Take(queue, "income-amount-2");
        Assert.Equal("transaction.income.amount", invalid.Payload.Frame.State);
        Assert.Equal("Enter a valid numeric amount", Error(invalid.Payload.Frame.StateData));
        await Publish(Input("actor-income-amount", "room-income-amount", "text", "1.23456", "income-amount-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> precise = await Take(queue, "income-amount-3");
        Assert.Equal("transaction.income.amount", precise.Payload.Frame.State);
        Assert.Equal("Enter up to 4 decimal places", Error(precise.Payload.Frame.StateData));
        await Publish(Input("actor-income-amount", "room-income-amount", "text", "0", "income-amount-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> zero = await Take(queue, "income-amount-4");
        Assert.Equal("transaction.income.amount", zero.Payload.Frame.State);
        Assert.Equal("Amount must be greater than zero", Error(zero.Payload.Frame.StateData));
    }
    /// <summary>
    /// Verifies that one idempotency key applies the income exactly once.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Processes the same income confirmation idempotency key once")]
    public async Task Deduplicates_income()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-dedupe", "room-income-dedupe", "Cash", "USD", "100", "income-dedupe-account");
        await Publish(Input("actor-income-dedupe", "room-income-dedupe", "action", "transaction.income.add", "income-dedupe-1"));
        _ = await Take(queue, "income-dedupe-1");
        await Publish(Input("actor-income-dedupe", "room-income-dedupe", "text", "10", "income-dedupe-2"));
        _ = await Take(queue, "income-dedupe-2");
        await Publish(Input("actor-income-dedupe", "room-income-dedupe", "action", "transaction.income.category.1", "income-dedupe-3"));
        _ = await Take(queue, "income-dedupe-3");
        MessageEnvelope<WorkspaceInputRequestedCommand> item = Input("actor-income-dedupe", "room-income-dedupe", "action", "transaction.income.create", "income-dedupe-4");
        await Publish(item);
        await Publish(item);
        _ = await Take(queue, "income-dedupe-4");
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry where kind = 'income'"));
        Assert.Equal(110m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Null(await View(queue, TimeSpan.FromSeconds(1)));
    }
    /// <summary>
    /// Verifies that repeated free text category names reuse the same user category.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Reuses one user category for repeated free text income input")]
    public async Task Reuses_category()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-category"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-category", "room-income-category", "Cash", "USD", "100", "income-category-account");
        await Record(queue, "actor-income-category", "room-income-category", "15", "Freelance", "income-category-one");
        await Record(queue, "actor-income-category", "room-income-category", "5", "freelance", "income-category-two");
        Assert.Equal(1, await Number("select count(*) from finance.category where scope = 'user' and kind = 'income' and user_id = (select id from finance.user_account where actor_key = 'actor-income-category')"));
        Assert.Equal(2, await Number("select count(*) from finance.transaction_entry where kind = 'income'"));
    }
    /// <summary>
    /// Verifies that a legacy home snapshot without account ids still records an income.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates an income from a legacy home snapshot without account ids")]
    public async Task Creates_income_from_legacy_home()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-income-legacy"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-income-legacy", "room-income-legacy", "Cash", "USD", "100", "income-legacy-account");
        await Execute("update finance.workspace set state_data = '{\"accounts\":[{\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":100}],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}'::jsonb where conversation_key = 'room-income-legacy'");
        await Publish(Input("actor-income-legacy", "room-income-legacy", "action", "transaction.income.add", "income-legacy-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "income-legacy-1");
        Assert.Equal("transaction.income.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-income-legacy", "room-income-legacy", "text", "11", "income-legacy-2"));
        _ = await Take(queue, "income-legacy-2");
        await Publish(Input("actor-income-legacy", "room-income-legacy", "action", "transaction.income.category.1", "income-legacy-3"));
        _ = await Take(queue, "income-legacy-3");
        await Publish(Input("actor-income-legacy", "room-income-legacy", "action", "transaction.income.create", "income-legacy-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "income-legacy-4");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal(1, await Number("select count(*) from finance.transaction_entry where kind = 'income'"));
        Assert.Equal(111m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
    }
    private async Task Record(string queue, string actor, string room, string amount, string category, string id)
    {
        await Publish(Input(actor, room, "action", "transaction.income.add", $"{id}-1"));
        _ = await Take(queue, $"{id}-1");
        await Publish(Input(actor, room, "text", amount, $"{id}-2"));
        _ = await Take(queue, $"{id}-2");
        await Publish(Input(actor, room, "text", category, $"{id}-3"));
        _ = await Take(queue, $"{id}-3");
        await Publish(Input(actor, room, "action", "transaction.income.create", $"{id}-4"));
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
        string? text = item.RootElement.GetProperty("status").GetProperty("notice").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing status.notice");
    }
    private static string Error(string data)
    {
        using var item = JsonDocument.Parse(data);
        string? text = item.RootElement.GetProperty("status").GetProperty("error").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing status.error");
    }
    private static string Account(string data, string name, string prefix)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement candidate in item.RootElement.GetProperty("choices").GetProperty("accounts").EnumerateArray())
        {
            string? text = candidate.GetProperty("name").GetString();
            int slot = candidate.GetProperty("slot").GetInt32();
            if (text == name)
            {
                return $"{prefix}{slot}";
            }
        }
        throw new InvalidOperationException($"Workspace state is missing account choice '{name}'");
    }
}
