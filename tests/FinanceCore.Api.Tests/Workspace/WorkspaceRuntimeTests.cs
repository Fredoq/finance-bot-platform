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
        Assert.True(view!.Payload.Freshness.IsNewUser);
        Assert.True(view.Payload.Freshness.IsNewWorkspace);
        Assert.Equal("home", view.Payload.Frame.State);
        Assert.Equal("promo-42", await Scalar("select last_payload from finance.workspace where conversation_key = 'room-1'"));
        Assert.Equal(0, Count(view.Payload.Frame.StateData, "accounts"));
        Assert.Equal(["account.add", "profile.timezone.show"], view.Payload.Frame.Actions);
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
        await Take(queue, "workspace-requested-2");
        await Publish(Input("actor-2", "room-2", "action", "account.add", "workspace-input-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? name = await View(queue);
        Assert.NotNull(name);
        Assert.Equal("account.name", name!.Payload.Frame.State);
        await Publish(Input("actor-2", "room-2", "text", "Cash", "workspace-input-3"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? currency = await View(queue);
        Assert.NotNull(currency);
        Assert.Equal("account.currency", currency!.Payload.Frame.State);
        await Publish(Input("actor-2", "room-2", "action", "account.currency.rub", "workspace-input-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? balance = await View(queue);
        Assert.NotNull(balance);
        Assert.Equal("account.balance", balance!.Payload.Frame.State);
        await Publish(Input("actor-2", "room-2", "text", "1500,50", "workspace-input-5"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? confirm = await View(queue);
        Assert.NotNull(confirm);
        Assert.Equal("account.confirm", confirm!.Payload.Frame.State);
        Assert.Equal(1500.50m, Amount(confirm.Payload.Frame.StateData));
        await Publish(Input("actor-2", "room-2", "action", "account.create", "workspace-input-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? home = await View(queue);
        Assert.NotNull(home);
        Assert.Equal("home", home!.Payload.Frame.State);
        Assert.Equal("Cash", Name(home.Payload.Frame.StateData));
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
        await Take(queue, "workspace-requested-3");
        await Publish(Input("actor-3", "room-3", "action", "account.add", "workspace-input-7"));
        await Take(queue, "workspace-input-7");
        await Publish(Input("actor-3", "room-3", "action", "account.cancel", "workspace-input-8"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? home = await View(queue);
        Assert.NotNull(home);
        Assert.Equal("home", home!.Payload.Frame.State);
        Assert.Equal(0, Count(home.Payload.Frame.StateData, "accounts"));
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
        await Take(queue, "workspace-requested-5");
        await Create(queue, "actor-5", "room-5", "Cash", "RUB", "10", "11");
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Create(queue, "actor-5", "room-5", "Cash", "USD", "20", "21");
        Assert.Equal("account.name", view.Payload.Frame.State);
        Assert.Equal("Cash", DraftName(view.Payload.Frame.StateData));
        Assert.Equal("USD", DraftCurrency(view.Payload.Frame.StateData));
        Assert.Equal(20m, Amount(view.Payload.Frame.StateData));
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
        await Take(queue, "workspace-requested-6");
        await Publish(Input("actor-6", "room-6", "action", "account.add", "workspace-input-22"));
        await Take(queue, "workspace-input-22");
        await Publish(Input("actor-6", "room-6", "text", "Card", "workspace-input-23"));
        await Take(queue, "workspace-input-23");
        await Publish(Input("actor-6", "room-6", "action", "account.currency.usd", "workspace-input-24"));
        await Take(queue, "workspace-input-24");
        await Publish(Input("actor-6", "room-6", "text", "-25.5", "workspace-input-25"));
        await Take(queue, "workspace-input-25");
        await Publish(Input("actor-6", "room-6", "action", "account.create", "workspace-input-26"));
        await Take(queue, "workspace-input-26");
        Assert.Equal(-25.5m, decimal.Parse(await Scalar("select current_amount::text from finance.account where user_id = (select id from finance.user_account where actor_key = 'actor-6')"), System.Globalization.CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that unsupported input kinds preserve the error when the workspace stays on home.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Preserves home error state for unsupported input kinds")]
    public async Task Preserves_home_error()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-home-error"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-7", "room-7", string.Empty, "workspace-requested-7"));
        await Take(queue, "workspace-requested-7");
        await Publish(Input("actor-7", "room-7", "voice", "hello", "workspace-input-27"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "workspace-input-27");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Input kind is not supported", Error(home.Payload.Frame.StateData));
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
    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Create(string queue, string actor, string room, string name, string currency, string balance, string id)
    {
        await Publish(Input(actor, room, "action", "account.add", $"workspace-input-{id}-1"));
        await Take(queue, $"workspace-input-{id}-1");
        await Publish(Input(actor, room, "text", name, $"workspace-input-{id}-2"));
        await Take(queue, $"workspace-input-{id}-2");
        await Publish(Currency(actor, room, currency, $"workspace-input-{id}-3"));
        await Take(queue, $"workspace-input-{id}-3");
        await Publish(Input(actor, room, "text", balance, $"workspace-input-{id}-4"));
        await Take(queue, $"workspace-input-{id}-4");
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
    private static int Count(string data, string key)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty(key).GetArrayLength();
    }
    private static decimal Amount(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("financial").GetProperty("amount").GetDecimal();
    }
    private static string Name(string data)
    {
        using var item = JsonDocument.Parse(data);
        string? text = item.RootElement.GetProperty("accounts")[0].GetProperty("name").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing accounts[0].name");
    }
    private static string DraftName(string data)
    {
        using var item = JsonDocument.Parse(data);
        string? text = item.RootElement.GetProperty("financial").GetProperty("name").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing financial.name");
    }
    private static string DraftCurrency(string data)
    {
        using var item = JsonDocument.Parse(data);
        string? text = item.RootElement.GetProperty("financial").GetProperty("currency").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing financial.currency");
    }
    private static string Error(string data)
    {
        using var item = JsonDocument.Parse(data);
        string? text = item.RootElement.GetProperty("status").GetProperty("error").GetString();
        return text ?? throw new InvalidOperationException("Workspace state is missing status.error");
    }
}
