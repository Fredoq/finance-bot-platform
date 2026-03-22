using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;
using RabbitMQ.Client;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers finance core behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class WorkspaceRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the first workspace request creates onboarding state and publishes a view.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Creates first account onboarding for the first request")]
    public async Task Creates_workspace()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-create"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-1", "room-1", "promo-42", "workspace-requested-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.True(view!.Payload.IsNewUser);
        Assert.True(view.Payload.IsNewWorkspace);
        Assert.Equal("home", view.Payload.State);
        Assert.Equal("promo-42", await Scalar("select last_payload from finance.workspace where conversation_key = 'room-1'"));
        Assert.Equal(0, Count(view.Payload.StateData, "accounts"));
        Assert.Equal(["account.add"], view.Payload.Actions);
        Assert.Equal(1, await Number("select count(*) from finance.user_account"));
        Assert.Equal(1, await Number("select count(*) from finance.workspace"));
        Assert.Equal(0, await Number("select count(*) from finance.account"));
    }
    /// <summary>
    /// Verifies that the first account flow creates one account and returns home.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Creates one account through the onboarding flow")]
    public async Task Creates_account()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-account"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-2", "room-2", string.Empty, "workspace-requested-2"));
        _ = await View(queue);
        await Publish(Input("actor-2", "room-2", "action", "account.add", "workspace-input-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? name = await View(queue);
        Assert.NotNull(name);
        Assert.Equal("account.name", name!.Payload.State);
        await Publish(Input("actor-2", "room-2", "text", "Cash", "workspace-input-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? currency = await View(queue);
        Assert.NotNull(currency);
        Assert.Equal("account.currency", currency!.Payload.State);
        await Publish(Input("actor-2", "room-2", "action", "account.currency.rub", "workspace-input-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? balance = await View(queue);
        Assert.NotNull(balance);
        Assert.Equal("account.balance", balance!.Payload.State);
        await Publish(Input("actor-2", "room-2", "text", "1500,50", "workspace-input-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? confirm = await View(queue);
        Assert.NotNull(confirm);
        Assert.Equal("account.confirm", confirm!.Payload.State);
        Assert.Equal(1500.50m, Amount(confirm.Payload.StateData));
        await Publish(Input("actor-2", "room-2", "action", "account.create", "workspace-input-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? home = await View(queue);
        Assert.NotNull(home);
        Assert.Equal("home", home!.Payload.State);
        Assert.Equal("Cash", Name(home.Payload.StateData));
        Assert.Equal(1, await Number("select count(*) from finance.account"));
        Assert.Equal("RUB", await Scalar("select currency_code from finance.account where user_id = (select id from finance.user_account where actor_key = 'actor-2')"));
    }
    /// <summary>
    /// Verifies that cancel returns the workspace to home without persisting accounts.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Cancels account onboarding without creating an account")]
    public async Task Cancels_account()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-cancel"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-3", "room-3", string.Empty, "workspace-requested-3"));
        _ = await View(queue);
        await Publish(Input("actor-3", "room-3", "action", "account.add", "workspace-input-7"));
        _ = await View(queue);
        await Publish(Input("actor-3", "room-3", "action", "account.cancel", "workspace-input-8"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? home = await View(queue);
        Assert.NotNull(home);
        Assert.Equal("home", home!.Payload.State);
        Assert.Contains("Account was cancelled", home.Payload.StateData.Replace("creation ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal(0, await Number("select count(*) from finance.account"));
    }
    /// <summary>
    /// Verifies that duplicate idempotency keys do not create duplicates.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Processes the same idempotency key once")]
    public async Task Deduplicates_workspace()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-4", "room-4", string.Empty, "workspace-requested-4"));
        await Publish(Envelope("actor-4", "room-4", string.Empty, "workspace-requested-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.Equal(1, await Number("select count(*) from finance.user_account"));
        Assert.Equal(1, await Number("select count(*) from finance.workspace"));
        Assert.Equal(1, await Number("select count(*) from finance.inbox_message"));
        Assert.Equal(1, await Number("select count(*) from finance.outbox_message"));
        Assert.Null(await View(queue, TimeSpan.FromSeconds(1)));
    }
    /// <summary>
    /// Verifies that duplicate account names are rejected without creating duplicates.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Rejects duplicate account names for one user")]
    public async Task Rejects_duplicate_name()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-duplicate-name"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-5", "room-5", string.Empty, "workspace-requested-5"));
        _ = await View(queue);
        _ = await Create(queue, "actor-5", "room-5", "Cash", "RUB", "10", "11");
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await Create(queue, "actor-5", "room-5", "Cash", "USD", "20", "21");
        Assert.NotNull(view);
        Assert.Equal("account.name", view!.Payload.State);
        Assert.Contains("Account name already exists", view.Payload.StateData, StringComparison.Ordinal);
        Assert.Equal(1, await Number("select count(*) from finance.account"));
    }
    /// <summary>
    /// Verifies that negative balances are persisted.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Stores negative opening balances")]
    public async Task Stores_negative_balance()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-negative-balance"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-6", "room-6", string.Empty, "workspace-requested-6"));
        _ = await View(queue);
        await Publish(Input("actor-6", "room-6", "action", "account.add", "workspace-input-22"));
        _ = await View(queue);
        await Publish(Input("actor-6", "room-6", "text", "Card", "workspace-input-23"));
        _ = await View(queue);
        await Publish(Input("actor-6", "room-6", "text", "USD", "workspace-input-24"));
        _ = await View(queue);
        await Publish(Input("actor-6", "room-6", "text", "-25.5", "workspace-input-25"));
        _ = await View(queue);
        await Publish(Input("actor-6", "room-6", "action", "account.create", "workspace-input-26"));
        _ = await View(queue);
        Assert.Equal(-25.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where user_id = (select id from finance.user_account where actor_key = 'actor-6')"), System.Globalization.CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that unknown contracts are moved to the dead queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Moves unsupported contracts to the dead queue")]
    public async Task Rejects_contract()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-unknown"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Publish("workspace.requested", "budget.unknown", Encoding.UTF8.GetBytes("{\"contract\":\"budget.unknown\"}"));
        BasicGetResult? data = await Dead();
        Assert.NotNull(data);
        Assert.Equal(0, await Number("select count(*) from finance.user_account"));
        Assert.Equal(0, await Number("select count(*) from finance.workspace"));
    }
    /// <summary>
    /// Verifies that malformed payloads are moved to the dead queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Moves malformed payloads to the dead queue")]
    public async Task Rejects_payload()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-payload"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Publish("workspace.requested", "workspace.requested", Encoding.UTF8.GetBytes("{"));
        BasicGetResult? data = await Dead();
        Assert.NotNull(data);
        Assert.Equal(0, await Number("select count(*) from finance.user_account"));
    }
    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>?> Create(string queue, string actor, string room, string name, string currency, string balance, string id)
    {
        await Publish(Input(actor, room, "action", "account.add", $"workspace-input-{id}-1"));
        _ = await View(queue);
        await Publish(Input(actor, room, "text", name, $"workspace-input-{id}-2"));
        _ = await View(queue);
        await Publish(Input(actor, room, "text", currency, $"workspace-input-{id}-3"));
        _ = await View(queue);
        await Publish(Input(actor, room, "text", balance, $"workspace-input-{id}-4"));
        _ = await View(queue);
        await Publish(Input(actor, room, "action", "account.create", $"workspace-input-{id}-5"));
        return await View(queue);
    }
    private static int Count(string data, string key)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty(key).GetArrayLength();
    }
    private static decimal Amount(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("amount").GetDecimal();
    }
    private static string Name(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("accounts")[0].GetProperty("name").GetString() ?? string.Empty;
    }
}
