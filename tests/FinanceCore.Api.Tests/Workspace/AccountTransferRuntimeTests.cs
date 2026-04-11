using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers account transfer workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class AccountTransferRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the home state exposes the transfer action only when multiple accounts exist.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Shows the transfer action on home when multiple accounts exist")]
    public async Task Shows_transfer_action()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-home"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        MessageEnvelope<WorkspaceViewRequestedCommand> one = await Create(queue, "actor-transfer-home", "room-transfer-home", "Cash", "USD", "100", "transfer-home-one");
        Assert.DoesNotContain("transfer.add", one.Payload.Frame.Actions, StringComparer.Ordinal);
        MessageEnvelope<WorkspaceViewRequestedCommand> two = await Create(queue, "actor-transfer-home", "room-transfer-home", "Card", "USD", "50", "transfer-home-two");
        Assert.Contains("transfer.add", two.Payload.Frame.Actions, StringComparer.Ordinal);
    }
    /// <summary>
    /// Verifies that a same-currency transfer updates both account balances.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Creates a same currency account transfer")]
    public async Task Creates_transfer()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-create"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-create", "room-transfer-create", "Cash", "USD", "100", "transfer-create-one");
        await Create(queue, "actor-transfer-create", "room-transfer-create", "Card", "USD", "50", "transfer-create-two");
        await Publish(Input("actor-transfer-create", "room-transfer-create", "action", "transfer.add", "transfer-create-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "transfer-create-1");
        Assert.Equal("transfer.source.account", source.Payload.Frame.State);
        string cash = Choice(source.Payload.Frame.StateData, "transfer.source.account.", "Cash");
        await Publish(Input("actor-transfer-create", "room-transfer-create", "action", cash, "transfer-create-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> target = await Take(queue, "transfer-create-2");
        Assert.Equal("transfer.target.account", target.Payload.Frame.State);
        Assert.DoesNotContain("Cash", Choices(target.Payload.Frame.StateData));
        string card = Choice(target.Payload.Frame.StateData, "transfer.target.account.", "Card");
        await Publish(Input("actor-transfer-create", "room-transfer-create", "action", card, "transfer-create-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, "transfer-create-3");
        Assert.Equal("transfer.amount", amount.Payload.Frame.State);
        await Publish(Input("actor-transfer-create", "room-transfer-create", "text", "25", "transfer-create-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> confirm = await Take(queue, "transfer-create-4");
        Assert.Equal("transfer.confirm", confirm.Payload.Frame.State);
        await Publish(Input("actor-transfer-create", "room-transfer-create", "action", "transfer.create", "transfer-create-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "transfer-create-5");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Transfer was recorded", Notice(home.Payload.Frame.StateData));
        Assert.Equal(1, await Number("select count(*) from finance.account_transfer"));
        Assert.Equal(0, await Number("select count(*) from finance.transaction_entry"));
        Assert.Equal(75m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Equal(75m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Card'"), CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that target account choices exclude other currencies and the source account.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Filters transfer targets by currency and account")]
    public async Task Filters_targets()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-targets"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-targets", "room-transfer-targets", "Cash", "USD", "100", "transfer-targets-one");
        await Create(queue, "actor-transfer-targets", "room-transfer-targets", "Card", "USD", "50", "transfer-targets-two");
        await Create(queue, "actor-transfer-targets", "room-transfer-targets", "Euro", "EUR", "70", "transfer-targets-three");
        await Publish(Input("actor-transfer-targets", "room-transfer-targets", "action", "transfer.add", "transfer-targets-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "transfer-targets-1");
        await Publish(Input("actor-transfer-targets", "room-transfer-targets", "action", Choice(source.Payload.Frame.StateData, "transfer.source.account.", "Cash"), "transfer-targets-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> target = await Take(queue, "transfer-targets-2");
        IReadOnlyList<string> choices = Choices(target.Payload.Frame.StateData);
        Assert.Contains("Card", choices);
        Assert.DoesNotContain("Cash", choices);
        Assert.DoesNotContain("Euro", choices);
    }
    /// <summary>
    /// Verifies that a source account without a same-currency target returns home with an error.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Rejects a transfer without a same currency target")]
    public async Task Rejects_missing_target()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-missing-target"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-missing-target", "room-transfer-missing-target", "Cash", "USD", "100", "transfer-missing-target-one");
        await Create(queue, "actor-transfer-missing-target", "room-transfer-missing-target", "Euro", "EUR", "70", "transfer-missing-target-two");
        await Publish(Input("actor-transfer-missing-target", "room-transfer-missing-target", "action", "transfer.add", "transfer-missing-target-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, "transfer-missing-target-1");
        await Publish(Input("actor-transfer-missing-target", "room-transfer-missing-target", "action", Choice(source.Payload.Frame.StateData, "transfer.source.account.", "Cash"), "transfer-missing-target-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "transfer-missing-target-2");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Target account with the same currency is required", Error(home.Payload.Frame.StateData));
        Assert.Equal(0, await Number("select count(*) from finance.account_transfer"));
    }
    /// <summary>
    /// Verifies that invalid and non-positive transfer amounts return different validation errors.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns distinct validation errors for invalid, precise, and non-positive transfer amounts")]
    public async Task Rejects_transfer_amount()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-amount"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-amount", "room-transfer-amount", "Cash", "USD", "100", "transfer-amount-one");
        await Create(queue, "actor-transfer-amount", "room-transfer-amount", "Card", "USD", "50", "transfer-amount-two");
        await Open(queue, "actor-transfer-amount", "room-transfer-amount", "transfer-amount");
        await Publish(Input("actor-transfer-amount", "room-transfer-amount", "text", "abc", "transfer-amount-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand> invalid = await Take(queue, "transfer-amount-4");
        Assert.Equal("Enter a valid numeric amount", Error(invalid.Payload.Frame.StateData));
        await Publish(Input("actor-transfer-amount", "room-transfer-amount", "text", "1.23456", "transfer-amount-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand> precise = await Take(queue, "transfer-amount-5");
        Assert.Equal("Enter up to 4 decimal places", Error(precise.Payload.Frame.StateData));
        await Publish(Input("actor-transfer-amount", "room-transfer-amount", "text", "0", "transfer-amount-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand> zero = await Take(queue, "transfer-amount-6");
        Assert.Equal("Amount must be greater than zero", Error(zero.Payload.Frame.StateData));
    }
    /// <summary>
    /// Verifies that one idempotency key applies the transfer exactly once.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Processes the same transfer confirmation idempotency key once")]
    public async Task Deduplicates_transfer()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-dedupe", "room-transfer-dedupe", "Cash", "USD", "100", "transfer-dedupe-one");
        await Create(queue, "actor-transfer-dedupe", "room-transfer-dedupe", "Card", "USD", "50", "transfer-dedupe-two");
        await Open(queue, "actor-transfer-dedupe", "room-transfer-dedupe", "transfer-dedupe");
        await Publish(Input("actor-transfer-dedupe", "room-transfer-dedupe", "text", "20", "transfer-dedupe-4"));
        _ = await Take(queue, "transfer-dedupe-4");
        MessageEnvelope<WorkspaceInputRequestedCommand> item = Input("actor-transfer-dedupe", "room-transfer-dedupe", "action", "transfer.create", "transfer-dedupe-5");
        await Publish(item);
        await Publish(item);
        _ = await Take(queue, "transfer-dedupe-5");
        Assert.Equal(1, await Number("select count(*) from finance.account_transfer"));
        Assert.Equal(80m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
        Assert.Equal(70m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Card'"), CultureInfo.InvariantCulture));
        Assert.Null(await View(queue, TimeSpan.FromSeconds(1)));
    }
    /// <summary>
    /// Verifies that transfers do not affect monthly income, expense, or category reports.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Excludes transfers from monthly reports")]
    public async Task Excludes_transfer_from_reports()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-transfer-reports"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-transfer-reports", "room-transfer-reports", "Cash", "USD", "100", "transfer-reports-one");
        await Create(queue, "actor-transfer-reports", "room-transfer-reports", "Card", "USD", "50", "transfer-reports-two");
        await Open(queue, "actor-transfer-reports", "room-transfer-reports", "transfer-reports");
        await Publish(Input("actor-transfer-reports", "room-transfer-reports", "text", "10", "transfer-reports-4"));
        _ = await Take(queue, "transfer-reports-4");
        await Publish(Input("actor-transfer-reports", "room-transfer-reports", "action", "transfer.create", "transfer-reports-5"));
        _ = await Take(queue, "transfer-reports-5");
        await Publish(Input("actor-transfer-reports", "room-transfer-reports", "action", "summary.month.show", "transfer-reports-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand> summary = await Take(queue, "transfer-reports-6");
        Assert.Equal(0, Count(summary.Payload.Frame.StateData, "summary"));
        await Publish(Input("actor-transfer-reports", "room-transfer-reports", "action", "category.month.show", "transfer-reports-7"));
        MessageEnvelope<WorkspaceViewRequestedCommand> breakdown = await Take(queue, "transfer-reports-7");
        Assert.Equal(0, Count(breakdown.Payload.Frame.StateData, "breakdown"));
    }
    private async Task Open(string queue, string actor, string room, string id)
    {
        await Publish(Input(actor, room, "action", "transfer.add", $"{id}-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> source = await Take(queue, $"{id}-1");
        await Publish(Input(actor, room, "action", Choice(source.Payload.Frame.StateData, "transfer.source.account.", "Cash"), $"{id}-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand> target = await Take(queue, $"{id}-2");
        await Publish(Input(actor, room, "action", Choice(target.Payload.Frame.StateData, "transfer.target.account.", "Card"), $"{id}-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand> amount = await Take(queue, $"{id}-3");
        Assert.Equal("transfer.amount", amount.Payload.Frame.State);
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
    private static string Choice(string data, string prefix, string name)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("choices").GetProperty("accounts").EnumerateArray())
        {
            if (string.Equals(node.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                return $"{prefix}{node.GetProperty("slot").GetInt32()}";
            }
        }
        throw new InvalidOperationException($"Workspace state is missing account choice '{name}'");
    }
    private static IReadOnlyList<string> Choices(string data)
    {
        using var item = JsonDocument.Parse(data);
        return [.. item.RootElement.GetProperty("choices").GetProperty("accounts").EnumerateArray().Select(node => node.GetProperty("name").GetString() ?? string.Empty)];
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
    private static int Count(string data, string name)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty(name).GetProperty("currencies").GetArrayLength();
    }
}
