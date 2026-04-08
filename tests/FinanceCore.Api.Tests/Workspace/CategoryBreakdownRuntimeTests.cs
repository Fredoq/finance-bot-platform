using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers category breakdown workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class CategoryBreakdownRuntimeTests : FinanceCoreRuntimeSuite
{
    private static readonly TimeSpan wait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan poll = TimeSpan.FromMilliseconds(250);
    /// <summary>
    /// Verifies that the category breakdown opens for the selected summary month and stays valid when empty.
    /// </summary>
    [Fact(DisplayName = "Opens an empty category breakdown from the selected summary month")]
    public async Task Shows_empty_breakdown()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-empty"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-breakdown-empty", "room-breakdown-empty", "Cash", "USD", "100", "breakdown-empty-account");
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Open(queue, "actor-breakdown-empty", "room-breakdown-empty", "breakdown-empty-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal("category.month", view.Payload.Frame.State);
        Assert.Equal(2026, Year(view.Payload.Frame.StateData));
        Assert.Equal(4, Month(view.Payload.Frame.StateData));
        Assert.Equal("Etc/UTC", TimeZone(view.Payload.Frame.StateData, "breakdown"));
        Assert.Equal(0, CurrencyCount(view.Payload.Frame.StateData));
        Assert.Equal(["category.month.prev", "category.month.back"], view.Payload.Frame.Actions);
    }

    /// <summary>
    /// Verifies that the category breakdown groups by currency, sorts categories, and computes shares.
    /// </summary>
    [Fact(DisplayName = "Builds category breakdown groups with sorted expense categories and shares")]
    public async Task Shows_breakdown_totals()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-totals"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-breakdown-totals", "room-breakdown-totals", "Cash", "USD", "100", "breakdown-totals-usd");
        await Create(queue, "actor-breakdown-totals", "room-breakdown-totals", "Wallet", "EUR", "50", "breakdown-totals-eur");
        await Record(queue, "actor-breakdown-totals", "room-breakdown-totals", new EntryNote("expense", "Cash", "30", "Food", "breakdown-totals-food", new DateTimeOffset(2026, 4, 5, 8, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-breakdown-totals", "room-breakdown-totals", new EntryNote("expense", "Cash", "10", "Travel", "breakdown-totals-travel", new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-breakdown-totals", "room-breakdown-totals", new EntryNote("expense", "Wallet", "7", "Travel", "breakdown-totals-eur-expense", new DateTimeOffset(2026, 4, 7, 9, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Open(queue, "actor-breakdown-totals", "room-breakdown-totals", "breakdown-totals-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(2, CurrencyCount(view.Payload.Frame.StateData));
        Assert.Equal("EUR", Currency(view.Payload.Frame.StateData, 0));
        Assert.Equal("USD", Currency(view.Payload.Frame.StateData, 1));
        Assert.Equal(7m, Total(view.Payload.Frame.StateData, "EUR"));
        Assert.Equal(40m, Total(view.Payload.Frame.StateData, "USD"));
        Assert.Equal("Food", Category(view.Payload.Frame.StateData, "USD", 0));
        Assert.Equal(30m, Amount(view.Payload.Frame.StateData, "USD", "Food"));
        Assert.Equal(0.75m, Share(view.Payload.Frame.StateData, "USD", "Food"));
        Assert.Equal("Travel", Category(view.Payload.Frame.StateData, "USD", 1));
        Assert.Equal(10m, Amount(view.Payload.Frame.StateData, "USD", "Travel"));
        Assert.Equal(0.25m, Share(view.Payload.Frame.StateData, "USD", "Travel"));
    }

    /// <summary>
    /// Verifies that category breakdown navigation moves backward and forward and can return to summary.
    /// </summary>
    [Fact(DisplayName = "Navigates category breakdown between months and returns to summary")]
    public async Task Navigates_breakdown()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-nav"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-breakdown-nav", "room-breakdown-nav", "Cash", "USD", "100", "breakdown-nav-account");
        await Record(queue, "actor-breakdown-nav", "room-breakdown-nav", new EntryNote("expense", "Cash", "11", "Food", "breakdown-nav-march", new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-breakdown-nav", "room-breakdown-nav", new EntryNote("expense", "Cash", "13", "Travel", "breakdown-nav-april", new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> april = await Open(queue, "actor-breakdown-nav", "room-breakdown-nav", "breakdown-nav-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(["category.month.prev", "category.month.back"], april.Payload.Frame.Actions);
        await Publish(InputAt("actor-breakdown-nav", "room-breakdown-nav", "action", "category.month.prev", "breakdown-nav-prev", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> march = await Take(queue, "breakdown-nav-prev");
        Assert.Equal(2026, Year(march.Payload.Frame.StateData));
        Assert.Equal(3, Month(march.Payload.Frame.StateData));
        Assert.Equal(["category.month.prev", "category.month.next", "category.month.back"], march.Payload.Frame.Actions);
        Assert.Equal(11m, Amount(march.Payload.Frame.StateData, "USD", "Food"));
        await Publish(InputAt("actor-breakdown-nav", "room-breakdown-nav", "action", "category.month.next", "breakdown-nav-next", new DateTimeOffset(2026, 4, 20, 12, 2, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> current = await Take(queue, "breakdown-nav-next");
        Assert.Equal(2026, Year(current.Payload.Frame.StateData));
        Assert.Equal(4, Month(current.Payload.Frame.StateData));
        Assert.Equal(["category.month.prev", "category.month.back"], current.Payload.Frame.Actions);
        await Publish(InputAt("actor-breakdown-nav", "room-breakdown-nav", "action", "category.month.back", "breakdown-nav-back", new DateTimeOffset(2026, 4, 20, 12, 3, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> summary = await Take(queue, "breakdown-nav-back");
        Assert.Equal("summary.month", summary.Payload.Frame.State);
        Assert.Equal(2026, SummaryYear(summary.Payload.Frame.StateData));
        Assert.Equal(4, SummaryMonth(summary.Payload.Frame.StateData));
        Assert.Contains("category.month.show", summary.Payload.Frame.Actions, StringComparer.Ordinal);
    }

    /// <summary>
    /// Verifies that text input on category breakdown keeps the screen and reports the expected error.
    /// </summary>
    [Fact(DisplayName = "Rejects free text on category breakdown and keeps the current month")]
    public async Task Rejects_text()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-text"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-breakdown-text", "room-breakdown-text", "Cash", "USD", "100", "breakdown-text-account");
        await Record(queue, "actor-breakdown-text", "room-breakdown-text", new EntryNote("expense", "Cash", "7", "Food", "breakdown-text-expense", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        _ = await Open(queue, "actor-breakdown-text", "room-breakdown-text", "breakdown-text-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        await Publish(InputAt("actor-breakdown-text", "room-breakdown-text", "text", "hello", "breakdown-text-value", new DateTimeOffset(2026, 4, 20, 12, 2, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> invalid = await Take(queue, "breakdown-text-value");
        Assert.Equal("category.month", invalid.Payload.Frame.State);
        Assert.Equal("Use the buttons to change the month or go back", Error(invalid.Payload.Frame.StateData));
        Assert.Equal(7m, Amount(invalid.Payload.Frame.StateData, "USD", "Food"));
    }

    /// <summary>
    /// Verifies that UTC month boundaries apply to category breakdown expenses.
    /// </summary>
    [Fact(DisplayName = "Counts category breakdown totals by UTC month boundary")]
    public async Task Uses_utc_boundary()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-boundary"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-breakdown-boundary", "room-breakdown-boundary", "Cash", "USD", "100", "breakdown-boundary-account");
        await Record(queue, "actor-breakdown-boundary", "room-breakdown-boundary", new EntryNote("expense", "Cash", "5", "Food", "breakdown-boundary-march", new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero)));
        await Record(queue, "actor-breakdown-boundary", "room-breakdown-boundary", new EntryNote("expense", "Cash", "9", "Travel", "breakdown-boundary-april", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> april = await Open(queue, "actor-breakdown-boundary", "room-breakdown-boundary", "breakdown-boundary-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(9m, Amount(april.Payload.Frame.StateData, "USD", "Travel"));
        await Publish(InputAt("actor-breakdown-boundary", "room-breakdown-boundary", "action", "category.month.prev", "breakdown-boundary-prev", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> march = await Take(queue, "breakdown-boundary-prev");
        Assert.Equal(5m, Amount(march.Payload.Frame.StateData, "USD", "Food"));
    }

    /// <summary>
    /// Verifies that the category breakdown uses the user's local expense month.
    /// </summary>
    [Fact(DisplayName = "Counts category breakdown totals by the user local month")]
    public async Task Uses_local_month()
    {
        const string zone = "Europe/Moscow";
        string queue = $"view-{Guid.CreateVersion7():N}";
        const string actor = "actor-breakdown-zone";
        const string room = "room-breakdown-zone";
        await using var host = new CoreApiFactory(Settings("finance-core-breakdown-zone"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Prepare(queue, actor, room, zone, new EntryNote("expense", "Cash", "5", "Food", "breakdown-zone-march", new DateTimeOffset(2026, 3, 31, 21, 30, 0, TimeSpan.Zero)), new EntryNote("expense", "Cash", "9", "Travel", "breakdown-zone-april", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> april = await Open(queue, actor, room, "breakdown-zone-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(2026, Year(april.Payload.Frame.StateData));
        Assert.Equal(4, Month(april.Payload.Frame.StateData));
        Assert.Equal(zone, TimeZone(april.Payload.Frame.StateData, "breakdown"));
        Assert.Equal(5m, Amount(april.Payload.Frame.StateData, "USD", "Food"));
        Assert.Equal(9m, Amount(april.Payload.Frame.StateData, "USD", "Travel"));
        await Publish(InputAt(actor, room, "action", "category.month.prev", "breakdown-zone-prev", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> march = await Take(queue, "breakdown-zone-prev");
        Assert.Equal(2026, Year(march.Payload.Frame.StateData));
        Assert.Equal(3, Month(march.Payload.Frame.StateData));
        Assert.Equal(zone, TimeZone(march.Payload.Frame.StateData, "breakdown"));
        Assert.Equal(0, CurrencyCount(march.Payload.Frame.StateData));
    }

    private async Task Prepare(string queue, string actor, string room, string zone, params EntryNote[] notes)
    {
        await Create(queue, actor, room, "Cash", "USD", "100", "breakdown-zone-account");
        await Zone(actor, zone);
        foreach (EntryNote note in notes)
        {
            await Record(queue, actor, room, note);
        }
    }

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Open(string queue, string actor, string room, string id, DateTimeOffset when)
    {
        await Publish(InputAt(actor, room, "action", "summary.month.show", $"{id}-summary", when));
        _ = await Take(queue, $"{id}-summary");
        await Publish(InputAt(actor, room, "action", "category.month.show", id, when.AddMinutes(1)));
        return await Take(queue, id);
    }

    private async Task Record(string queue, string actor, string room, EntryNote note)
    {
        string add;
        string prefix;
        string category;
        string create;
        switch (note.Kind)
        {
            case "income":
                add = "transaction.income.add";
                prefix = "transaction.income.account.";
                category = "transaction.income.category";
                create = "transaction.income.create";
                break;
            case "expense":
                add = "transaction.expense.add";
                prefix = "transaction.expense.account.";
                category = "transaction.expense.category";
                create = "transaction.expense.create";
                break;
            default:
                throw new InvalidOperationException($"Workspace entry kind '{note.Kind}' is not supported");
        }
        await Publish(Input(actor, room, "action", add, $"{note.Id}-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Take(queue, $"{note.Id}-1");
        if (view.Payload.Frame.State.EndsWith(".account", StringComparison.Ordinal))
        {
            string code = Account(view.Payload.Frame.StateData, note.AccountName, prefix);
            await Publish(Input(actor, room, "action", code, $"{note.Id}-2"));
            _ = await Take(queue, $"{note.Id}-2");
            await Publish(Input(actor, room, "text", note.TotalText, $"{note.Id}-3"));
            _ = await Take(queue, $"{note.Id}-3");
            await Publish(Input(actor, room, "text", note.Id, $"{note.Id}-4"));
            view = await Take(queue, $"{note.Id}-4");
            if (string.Equals(view.Payload.Frame.State, category, StringComparison.Ordinal))
            {
                await Publish(Input(actor, room, "text", note.CategoryName, $"{note.Id}-5"));
                _ = await Take(queue, $"{note.Id}-5");
                await Publish(InputAt(actor, room, "action", create, $"{note.Id}-6", note.When));
                _ = await Take(queue, $"{note.Id}-6");
                return;
            }
            await Publish(InputAt(actor, room, "action", create, $"{note.Id}-5", note.When));
            _ = await Take(queue, $"{note.Id}-5");
            return;
        }
        await Publish(Input(actor, room, "text", note.TotalText, $"{note.Id}-2"));
        _ = await Take(queue, $"{note.Id}-2");
        await Publish(Input(actor, room, "text", note.Id, $"{note.Id}-3"));
        view = await Take(queue, $"{note.Id}-3");
        if (string.Equals(view.Payload.Frame.State, category, StringComparison.Ordinal))
        {
            await Publish(Input(actor, room, "text", note.CategoryName, $"{note.Id}-4"));
            _ = await Take(queue, $"{note.Id}-4");
            await Publish(InputAt(actor, room, "action", create, $"{note.Id}-5", note.When));
            _ = await Take(queue, $"{note.Id}-5");
            return;
        }
        await Publish(InputAt(actor, room, "action", create, $"{note.Id}-4", note.When));
        _ = await Take(queue, $"{note.Id}-4");
    }

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Create(string queue, string actor, string room, string name, string currency, string balance, string id)
    {
        await Publish(Envelope(actor, room, string.Empty, $"workspace-requested-{id}"));
        _ = await Take(queue, $"workspace-requested-{id}");
        await Publish(Input(actor, room, "action", "account.add", $"workspace-input-{id}-1"));
        _ = await Take(queue, $"workspace-input-{id}-1");
        await Publish(Input(actor, room, "text", name, $"workspace-input-{id}-2"));
        _ = await Take(queue, $"workspace-input-{id}-2");
        await Publish(CurrencyInput(actor, room, currency, $"workspace-input-{id}-3"));
        _ = await Take(queue, $"workspace-input-{id}-3");
        await Publish(Input(actor, room, "text", balance, $"workspace-input-{id}-4"));
        _ = await Take(queue, $"workspace-input-{id}-4");
        await Publish(Input(actor, room, "action", "account.create", $"workspace-input-{id}-5"));
        return await Take(queue, $"workspace-input-{id}-5");
    }

    private static MessageEnvelope<WorkspaceInputRequestedCommand> CurrencyInput(string actor, string room, string currency, string id)
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

    private static MessageEnvelope<WorkspaceInputRequestedCommand> InputAt(string actor, string room, string kind, string value, string id, DateTimeOffset when) => new(
        Guid.CreateVersion7(),
        "workspace.input.requested",
        when,
        new MessageContext($"trace-{Guid.CreateVersion7():N}", $"cause-{Guid.CreateVersion7():N}", id),
        "telegram-gateway",
        new WorkspaceInputRequestedCommand(new WorkspaceIdentity(actor, room), new WorkspaceProfile("Alex", "en"), kind, value, when));

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Take(string queue, string step)
    {
        string key = $"{step}:workspace-view";
        DateTimeOffset until = DateTimeOffset.UtcNow.Add(wait);
        while (DateTimeOffset.UtcNow < until)
        {
            MessageEnvelope<WorkspaceViewRequestedCommand>? item = await View(queue, poll);
            if (item is null)
            {
                continue;
            }
            if (string.Equals(item.Context.IdempotencyKey, key, StringComparison.Ordinal))
            {
                return item;
            }
            throw new InvalidOperationException($"Workspace view '{item.Context.IdempotencyKey}' was received instead of '{key}' after '{step}'");
        }
        throw new InvalidOperationException($"Workspace view is missing after '{step}'");
    }

    private static string Error(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("status").GetProperty("error").GetString() ?? string.Empty;
    }

    private static int Year(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("breakdown").GetProperty("year").GetInt32();
    }

    private static int Month(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("breakdown").GetProperty("month").GetInt32();
    }

    private static int SummaryYear(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("year").GetInt32();
    }

    private static int SummaryMonth(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("month").GetInt32();
    }

    private static int CurrencyCount(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("breakdown").GetProperty("currencies").GetArrayLength();
    }

    private static string Currency(string data, int index)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("breakdown").GetProperty("currencies")[index].GetProperty("currency").GetString() ?? string.Empty;
    }

    private static decimal Total(string data, string currency)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("breakdown").GetProperty("currencies").EnumerateArray())
        {
            if (string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                return decimal.Parse(node.GetProperty("total").GetRawText(), CultureInfo.InvariantCulture);
            }
        }
        throw new InvalidOperationException($"Workspace breakdown is missing currency '{currency}'");
    }

    private static string Category(string data, string currency, int index)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("breakdown").GetProperty("currencies").EnumerateArray())
        {
            if (string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                return node.GetProperty("categories")[index].GetProperty("name").GetString() ?? string.Empty;
            }
        }
        throw new InvalidOperationException($"Workspace breakdown is missing currency '{currency}'");
    }

    private static decimal Amount(string data, string currency, string category)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("breakdown").GetProperty("currencies").EnumerateArray())
        {
            if (!string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (JsonElement current in node.GetProperty("categories").EnumerateArray())
            {
                if (string.Equals(current.GetProperty("name").GetString(), category, StringComparison.Ordinal))
                {
                    return decimal.Parse(current.GetProperty("amount").GetRawText(), CultureInfo.InvariantCulture);
                }
            }
        }
        throw new InvalidOperationException($"Workspace breakdown is missing category '{category}' for '{currency}'");
    }

    private static decimal Share(string data, string currency, string category)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("breakdown").GetProperty("currencies").EnumerateArray())
        {
            if (!string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (JsonElement current in node.GetProperty("categories").EnumerateArray())
            {
                if (string.Equals(current.GetProperty("name").GetString(), category, StringComparison.Ordinal))
                {
                    return decimal.Parse(current.GetProperty("share").GetRawText(), CultureInfo.InvariantCulture);
                }
            }
        }
        throw new InvalidOperationException($"Workspace breakdown is missing category '{category}' for '{currency}'");
    }

    private static string Account(string data, string name, string prefix)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement candidate in item.RootElement.GetProperty("choices").GetProperty("accounts").EnumerateArray())
        {
            if (string.Equals(candidate.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                return $"{prefix}{candidate.GetProperty("slot").GetInt32()}";
            }
        }
        throw new InvalidOperationException($"Workspace state is missing account choice '{name}'");
    }

    private sealed record EntryNote
    {
        internal EntryNote(string kind, string account, string amount, string category, string id, DateTimeOffset when)
        {
            Kind = kind;
            AccountName = account;
            TotalText = amount;
            CategoryName = category;
            Id = id;
            When = when;
        }
        public string Kind { get; }
        public string AccountName { get; }
        public string TotalText { get; }
        public string CategoryName { get; }
        public string Id { get; }
        public DateTimeOffset When { get; }
    }
}
