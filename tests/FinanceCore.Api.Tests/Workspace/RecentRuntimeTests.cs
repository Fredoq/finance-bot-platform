using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers recent transaction workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class RecentRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that home exposes the recent action when an account exists.
    /// </summary>
    [Fact(DisplayName = "Shows the recent transactions action on home when an account exists")]
    public async Task Shows_recent_action()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-home"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Create(queue, "actor-recent-home", "room-recent-home", "Cash", "USD", "100", "recent-home");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Contains("transaction.recent.show", home.Payload.Frame.Actions, StringComparer.Ordinal);
    }
    /// <summary>
    /// Verifies that recent transactions are paged and newest first.
    /// </summary>
    [Fact(DisplayName = "Shows the newest recent transactions with paging actions")]
    public async Task Shows_recent_page()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-page"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-page", "room-recent-page", "Cash", "USD", "100", "recent-page");
        for (int item = 0; item < 6; item += 1)
        {
            await Record(queue, "actor-recent-page", "room-recent-page", (item + 1).ToString(CultureInfo.InvariantCulture), $"Coffee {item}", $"recent-page-{item}");
        }
        await Publish(Input("actor-recent-page", "room-recent-page", "action", "transaction.recent.show", "recent-page-open"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-page-open");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Contains("transaction.recent.item.1", list.Payload.Frame.Actions, StringComparer.Ordinal);
        Assert.Contains("transaction.recent.item.5", list.Payload.Frame.Actions, StringComparer.Ordinal);
        Assert.Contains("transaction.recent.page.next", list.Payload.Frame.Actions, StringComparer.Ordinal);
        Assert.DoesNotContain("transaction.recent.page.prev", list.Payload.Frame.Actions, StringComparer.Ordinal);
        Assert.Equal(5, Count(list.Payload.Frame.StateData, "items"));
        Assert.Equal("Coffee 5", CategoryName(list.Payload.Frame.StateData, 0));
        Assert.Equal("Coffee 1", CategoryName(list.Payload.Frame.StateData, 4));
    }
    /// <summary>
    /// Verifies that deleting on a non-empty page keeps the current page selected.
    /// </summary>
    [Fact(DisplayName = "Keeps the current recent page after deleting from a non-empty page")]
    public async Task Keeps_recent_page_after_delete()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-delete-page"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-delete-page", "room-recent-delete-page", "Cash", "USD", "100", "recent-delete-page");
        for (int item = 0; item < 10; item += 1)
        {
            await Record(queue, "actor-recent-delete-page", "room-recent-delete-page", (item + 1).ToString(CultureInfo.InvariantCulture), $"Coffee {item}", $"recent-delete-page-{item}");
        }
        await Publish(Input("actor-recent-delete-page", "room-recent-delete-page", "action", "transaction.recent.show", "recent-delete-page-open"));
        _ = await Take(queue, "recent-delete-page-open");
        await Publish(Input("actor-recent-delete-page", "room-recent-delete-page", "action", "transaction.recent.page.next", "recent-delete-page-next"));
        MessageEnvelope<WorkspaceViewRequestedCommand> page = await Take(queue, "recent-delete-page-next");
        Assert.Equal(1, Page(page.Payload.Frame.StateData));
        await Publish(Input("actor-recent-delete-page", "room-recent-delete-page", "action", "transaction.recent.item.1", "recent-delete-page-item"));
        _ = await Take(queue, "recent-delete-page-item");
        await Publish(Input("actor-recent-delete-page", "room-recent-delete-page", "action", "transaction.recent.delete", "recent-delete-page-confirm"));
        _ = await Take(queue, "recent-delete-page-confirm");
        await Publish(Input("actor-recent-delete-page", "room-recent-delete-page", "action", "transaction.recent.delete.apply", "recent-delete-page-apply"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-delete-page-apply");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Equal("Transaction was deleted", Notice(list.Payload.Frame.StateData));
        Assert.Equal(1, Page(list.Payload.Frame.StateData));
        Assert.Equal(4, Count(list.Payload.Frame.StateData, "items"));
        Assert.Equal("Coffee 3", CategoryName(list.Payload.Frame.StateData, 0));
    }
    /// <summary>
    /// Verifies that deleting a recent transaction restores the account balance.
    /// </summary>
    [Fact(DisplayName = "Deletes a recent transaction and restores the account balance")]
    public async Task Deletes_recent_transaction()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-delete"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-delete", "room-recent-delete", "Cash", "USD", "100", "recent-delete");
        await Record(queue, "actor-recent-delete", "room-recent-delete", "10", "Coffee", "recent-delete-entry");
        await Publish(Input("actor-recent-delete", "room-recent-delete", "action", "transaction.recent.show", "recent-delete-open"));
        _ = await Take(queue, "recent-delete-open");
        await Publish(Input("actor-recent-delete", "room-recent-delete", "action", "transaction.recent.item.1", "recent-delete-item"));
        _ = await Take(queue, "recent-delete-item");
        await Publish(Input("actor-recent-delete", "room-recent-delete", "action", "transaction.recent.delete", "recent-delete-confirm"));
        _ = await Take(queue, "recent-delete-confirm");
        await Publish(Input("actor-recent-delete", "room-recent-delete", "action", "transaction.recent.delete.apply", "recent-delete-apply"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-delete-apply");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Equal("Transaction was deleted", Notice(list.Payload.Frame.StateData));
        Assert.Equal(0, await Number("select count(*) from finance.transaction_entry"));
        Assert.Equal(100m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that recategorizing with free text reuses the user category rules.
    /// </summary>
    [Fact(DisplayName = "Recategorizes a recent transaction from free text input")]
    public async Task Recategorizes_recent_transaction()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-recategorize"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-recategorize", "room-recent-recategorize", "Cash", "USD", "100", "recent-recategorize");
        await Record(queue, "actor-recent-recategorize", "room-recent-recategorize", "10", "Coffee", "recent-recategorize-entry");
        await Publish(Input("actor-recent-recategorize", "room-recent-recategorize", "action", "transaction.recent.show", "recent-recategorize-open"));
        _ = await Take(queue, "recent-recategorize-open");
        await Publish(Input("actor-recent-recategorize", "room-recent-recategorize", "action", "transaction.recent.item.1", "recent-recategorize-item"));
        _ = await Take(queue, "recent-recategorize-item");
        await Publish(Input("actor-recent-recategorize", "room-recent-recategorize", "action", "transaction.recent.recategorize", "recent-recategorize-category"));
        _ = await Take(queue, "recent-recategorize-category");
        await Publish(Input("actor-recent-recategorize", "room-recent-recategorize", "text", "Tea", "recent-recategorize-text"));
        _ = await Take(queue, "recent-recategorize-text");
        await Publish(Input("actor-recent-recategorize", "room-recent-recategorize", "action", "transaction.recent.recategorize.apply", "recent-recategorize-apply"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-recategorize-apply");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Equal("Category was updated", Notice(list.Payload.Frame.StateData));
        Assert.Equal("Tea", await Scalar("select c.name from finance.transaction_entry t join finance.category c on c.id = t.category_id where t.user_id = (select id from finance.user_account where actor_key = 'actor-recent-recategorize')"));
        Assert.Equal(90m, decimal.Parse(await Scalar("select current_amount::text from finance.account where name = 'Cash'"), CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that stale selected transactions fall back to a refreshed list.
    /// </summary>
    [Fact(DisplayName = "Returns to the recent list when the selected transaction no longer exists")]
    public async Task Handles_stale_recent_transaction()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-stale"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-stale", "room-recent-stale", "Cash", "USD", "100", "recent-stale");
        await Record(queue, "actor-recent-stale", "room-recent-stale", "10", "Coffee", "recent-stale-entry");
        await Publish(Input("actor-recent-stale", "room-recent-stale", "action", "transaction.recent.show", "recent-stale-open"));
        _ = await Take(queue, "recent-stale-open");
        await Publish(Input("actor-recent-stale", "room-recent-stale", "action", "transaction.recent.item.1", "recent-stale-item"));
        _ = await Take(queue, "recent-stale-item");
        await Publish(Input("actor-recent-stale", "room-recent-stale", "action", "transaction.recent.delete", "recent-stale-confirm"));
        _ = await Take(queue, "recent-stale-confirm");
        await Execute("delete from finance.transaction_entry");
        await Publish(Input("actor-recent-stale", "room-recent-stale", "action", "transaction.recent.delete.apply", "recent-stale-apply"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-stale-apply");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Equal("Transaction was not found", Notice(list.Payload.Frame.StateData));
    }
    /// <summary>
    /// Verifies that stale detail selection returns the last non-empty page.
    /// </summary>
    [Fact(DisplayName = "Returns the last non-empty recent page when a stale selection comes from the last page")]
    public async Task Handles_stale_recent_page()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-recent-stale-page"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-recent-stale-page", "room-recent-stale-page", "Cash", "USD", "100", "recent-stale-page");
        for (int item = 0; item < 6; item += 1)
        {
            await Record(queue, "actor-recent-stale-page", "room-recent-stale-page", (item + 1).ToString(CultureInfo.InvariantCulture), $"Coffee {item}", $"recent-stale-page-{item}");
        }
        await Publish(Input("actor-recent-stale-page", "room-recent-stale-page", "action", "transaction.recent.show", "recent-stale-page-open"));
        _ = await Take(queue, "recent-stale-page-open");
        await Publish(Input("actor-recent-stale-page", "room-recent-stale-page", "action", "transaction.recent.page.next", "recent-stale-page-next"));
        MessageEnvelope<WorkspaceViewRequestedCommand> page = await Take(queue, "recent-stale-page-next");
        Assert.Equal(1, Page(page.Payload.Frame.StateData));
        await Execute("delete from finance.transaction_entry where amount = 1");
        await Publish(Input("actor-recent-stale-page", "room-recent-stale-page", "action", "transaction.recent.item.1", "recent-stale-page-item"));
        MessageEnvelope<WorkspaceViewRequestedCommand> list = await Take(queue, "recent-stale-page-item");
        Assert.Equal("transaction.recent.list", list.Payload.Frame.State);
        Assert.Equal("Transaction was not found", Notice(list.Payload.Frame.StateData));
        Assert.Equal(0, Page(list.Payload.Frame.StateData));
        Assert.Equal(5, Count(list.Payload.Frame.StateData, "items"));
        Assert.Equal("Coffee 5", CategoryName(list.Payload.Frame.StateData, 0));
    }
    private async Task Record(string queue, string actor, string room, string amount, string category, string id)
    {
        await Publish(Input(actor, room, "action", "transaction.expense.add", $"{id}-1"));
        _ = await Take(queue, $"{id}-1");
        await Publish(Input(actor, room, "text", amount, $"{id}-2"));
        _ = await Take(queue, $"{id}-2");
        await Publish(Input(actor, room, "text", category, $"{id}-3"));
        _ = await Take(queue, $"{id}-3");
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
        => await View(queue) ?? throw new InvalidOperationException($"Missing workspace view for step '{step}'");
    private static string Notice(string state)
    {
        using var item = JsonDocument.Parse(state);
        return item.RootElement.GetProperty("status").GetProperty("notice").GetString() ?? string.Empty;
    }
    private static int Count(string state, string name)
    {
        using var item = JsonDocument.Parse(state);
        return item.RootElement.GetProperty("recent").GetProperty(name).GetArrayLength();
    }
    private static string CategoryName(string state, int index)
    {
        using var item = JsonDocument.Parse(state);
        return item.RootElement.GetProperty("recent").GetProperty("items")[index].GetProperty("category").GetProperty("name").GetString() ?? string.Empty;
    }
    private static int Page(string state)
    {
        using var item = JsonDocument.Parse(state);
        return item.RootElement.GetProperty("recent").GetProperty("page").GetInt32();
    }
}
