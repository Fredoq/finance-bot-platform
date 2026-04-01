using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers monthly summary workspace behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class MonthlySummaryRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that home exposes the summary action and that an empty month stays valid.
    /// </summary>
    [Fact(DisplayName = "Shows the monthly summary action and renders an empty month")]
    public async Task Shows_empty_summary()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-empty"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Create(queue, "actor-summary-empty", "room-summary-empty", "Cash", "USD", "100", "summary-empty-account");
        Assert.Contains("summary.month.show", home.Payload.Frame.Actions, StringComparer.Ordinal);
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Open(queue, "actor-summary-empty", "room-summary-empty", "summary-empty-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal("summary.month", view.Payload.Frame.State);
        Assert.Equal(2026, Year(view.Payload.Frame.StateData));
        Assert.Equal(4, Month(view.Payload.Frame.StateData));
        Assert.Equal(0, CurrencyCount(view.Payload.Frame.StateData));
        Assert.Equal(["summary.month.prev", "summary.month.back"], view.Payload.Frame.Actions);
    }

    /// <summary>
    /// Verifies that the current month summary aggregates totals and account rows in one currency.
    /// </summary>
    [Fact(DisplayName = "Builds monthly summary totals and account breakdown for one currency")]
    public async Task Shows_summary_totals()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-totals"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-summary-totals", "room-summary-totals", "Cash", "USD", "100", "summary-totals-cash");
        await Create(queue, "actor-summary-totals", "room-summary-totals", "Card", "USD", "50", "summary-totals-card");
        await Record(queue, "actor-summary-totals", "room-summary-totals", new EntryNote("income", "Cash", "100", "Salary", "summary-totals-income", new DateTimeOffset(2026, 4, 5, 8, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-summary-totals", "room-summary-totals", new EntryNote("expense", "Card", "40", "Food", "summary-totals-expense", new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Open(queue, "actor-summary-totals", "room-summary-totals", "summary-totals-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(1, CurrencyCount(view.Payload.Frame.StateData));
        Assert.Equal(100m, Total(view.Payload.Frame.StateData, "USD", "income"));
        Assert.Equal(40m, Total(view.Payload.Frame.StateData, "USD", "expense"));
        Assert.Equal(60m, Total(view.Payload.Frame.StateData, "USD", "net"));
        Assert.Equal(0m, AccountTotal(view.Payload.Frame.StateData, "USD", "Card", "income"));
        Assert.Equal(40m, AccountTotal(view.Payload.Frame.StateData, "USD", "Card", "expense"));
        Assert.Equal(100m, AccountTotal(view.Payload.Frame.StateData, "USD", "Cash", "income"));
        Assert.Equal(0m, AccountTotal(view.Payload.Frame.StateData, "USD", "Cash", "expense"));
    }

    /// <summary>
    /// Verifies that the summary keeps currencies separated without a cross-currency total.
    /// </summary>
    [Fact(DisplayName = "Builds monthly summary groups by currency")]
    public async Task Shows_summary_by_currency()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-currency"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-summary-currency", "room-summary-currency", "Cash", "USD", "100", "summary-currency-usd");
        await Create(queue, "actor-summary-currency", "room-summary-currency", "Wallet", "EUR", "50", "summary-currency-eur");
        await Record(queue, "actor-summary-currency", "room-summary-currency", new EntryNote("income", "Cash", "20", "Salary", "summary-currency-income", new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-summary-currency", "room-summary-currency", new EntryNote("expense", "Wallet", "7", "Travel", "summary-currency-expense", new DateTimeOffset(2026, 4, 4, 10, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Open(queue, "actor-summary-currency", "room-summary-currency", "summary-currency-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(2, CurrencyCount(view.Payload.Frame.StateData));
        Assert.Equal("EUR", Currency(view.Payload.Frame.StateData, 0));
        Assert.Equal("USD", Currency(view.Payload.Frame.StateData, 1));
        Assert.Equal(0m, Total(view.Payload.Frame.StateData, "EUR", "income"));
        Assert.Equal(7m, Total(view.Payload.Frame.StateData, "EUR", "expense"));
        Assert.Equal(20m, Total(view.Payload.Frame.StateData, "USD", "income"));
        Assert.Equal(0m, Total(view.Payload.Frame.StateData, "USD", "expense"));
    }

    /// <summary>
    /// Verifies that summary month navigation moves backward and forward with the correct next action.
    /// </summary>
    [Fact(DisplayName = "Navigates monthly summary between months")]
    public async Task Navigates_summary()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-nav"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-summary-nav", "room-summary-nav", "Cash", "USD", "100", "summary-nav-account");
        await Record(queue, "actor-summary-nav", "room-summary-nav", new EntryNote("income", "Cash", "11", "Salary", "summary-nav-march", new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero)));
        await Record(queue, "actor-summary-nav", "room-summary-nav", new EntryNote("income", "Cash", "13", "Salary", "summary-nav-april", new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> april = await Open(queue, "actor-summary-nav", "room-summary-nav", "summary-nav-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(["summary.month.prev", "summary.month.back"], april.Payload.Frame.Actions);
        await Publish(InputAt("actor-summary-nav", "room-summary-nav", "action", "summary.month.prev", "summary-nav-prev", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> march = await Take(queue, "summary-nav-prev");
        Assert.Equal(2026, Year(march.Payload.Frame.StateData));
        Assert.Equal(3, Month(march.Payload.Frame.StateData));
        Assert.Equal(["summary.month.prev", "summary.month.next", "summary.month.back"], march.Payload.Frame.Actions);
        Assert.Equal(11m, Total(march.Payload.Frame.StateData, "USD", "income"));
        await Publish(InputAt("actor-summary-nav", "room-summary-nav", "action", "summary.month.next", "summary-nav-next", new DateTimeOffset(2026, 4, 20, 12, 2, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> current = await Take(queue, "summary-nav-next");
        Assert.Equal(2026, Year(current.Payload.Frame.StateData));
        Assert.Equal(4, Month(current.Payload.Frame.StateData));
        Assert.Equal(["summary.month.prev", "summary.month.back"], current.Payload.Frame.Actions);
    }

    /// <summary>
    /// Verifies that summary back returns to home and that text input stays on summary with guidance.
    /// </summary>
    [Fact(DisplayName = "Returns home from monthly summary and rejects plain text input")]
    public async Task Returns_and_rejects_text()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-text"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-summary-text", "room-summary-text", "Cash", "USD", "100", "summary-text-account");
        _ = await Open(queue, "actor-summary-text", "room-summary-text", "summary-text-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        await Publish(InputAt("actor-summary-text", "room-summary-text", "text", "hello", "summary-text-value", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> invalid = await Take(queue, "summary-text-value");
        Assert.Equal("summary.month", invalid.Payload.Frame.State);
        Assert.Equal("Use the buttons to change the month or go back", Error(invalid.Payload.Frame.StateData));
        await Publish(InputAt("actor-summary-text", "room-summary-text", "action", "summary.month.back", "summary-text-back", new DateTimeOffset(2026, 4, 20, 12, 2, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "summary-text-back");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Choose the next action", Notice(home.Payload.Frame.StateData));
    }

    /// <summary>
    /// Verifies that UTC month boundaries exclude the last second of the previous month.
    /// </summary>
    [Fact(DisplayName = "Counts monthly summary totals by UTC month boundary")]
    public async Task Uses_utc_boundary()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-summary-boundary"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-summary-boundary", "room-summary-boundary", "Cash", "USD", "100", "summary-boundary-account");
        await Record(queue, "actor-summary-boundary", "room-summary-boundary", new EntryNote("expense", "Cash", "5", "Food", "summary-boundary-march", new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero)));
        await Record(queue, "actor-summary-boundary", "room-summary-boundary", new EntryNote("income", "Cash", "9", "Salary", "summary-boundary-april", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> april = await Open(queue, "actor-summary-boundary", "room-summary-boundary", "summary-boundary-open", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        Assert.Equal(9m, Total(april.Payload.Frame.StateData, "USD", "income"));
        Assert.Equal(0m, Total(april.Payload.Frame.StateData, "USD", "expense"));
        await Publish(InputAt("actor-summary-boundary", "room-summary-boundary", "action", "summary.month.prev", "summary-boundary-prev", new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> march = await Take(queue, "summary-boundary-prev");
        Assert.Equal(0m, Total(march.Payload.Frame.StateData, "USD", "income"));
        Assert.Equal(5m, Total(march.Payload.Frame.StateData, "USD", "expense"));
    }

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Open(string queue, string actor, string room, string id, DateTimeOffset when)
    {
        await Publish(InputAt(actor, room, "action", "summary.month.show", id, when));
        return await Take(queue, id);
    }

    private async Task Record(string queue, string actor, string room, EntryNote note)
    {
        string add = note.Kind == "income" ? "transaction.income.add" : "transaction.expense.add";
        string prefix = note.Kind == "income" ? "transaction.income.account." : "transaction.expense.account.";
        string create = note.Kind == "income" ? "transaction.income.create" : "transaction.expense.create";
        await Publish(Input(actor, room, "action", add, $"{note.Id}-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Take(queue, $"{note.Id}-1");
        if (view.Payload.Frame.State.EndsWith(".account", StringComparison.Ordinal))
        {
            string code = Account(view.Payload.Frame.StateData, note.AccountName, prefix);
            await Publish(Input(actor, room, "action", code, $"{note.Id}-2"));
            _ = await Take(queue, $"{note.Id}-2");
            await Publish(Input(actor, room, "text", note.Amount, $"{note.Id}-3"));
            _ = await Take(queue, $"{note.Id}-3");
            await Publish(Input(actor, room, "text", note.Category, $"{note.Id}-4"));
            _ = await Take(queue, $"{note.Id}-4");
            await Publish(InputAt(actor, room, "action", create, $"{note.Id}-5", note.When));
            _ = await Take(queue, $"{note.Id}-5");
            return;
        }
        await Publish(Input(actor, room, "text", note.Amount, $"{note.Id}-2"));
        _ = await Take(queue, $"{note.Id}-2");
        await Publish(Input(actor, room, "text", note.Category, $"{note.Id}-3"));
        _ = await Take(queue, $"{note.Id}-3");
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
        DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < until)
        {
            MessageEnvelope<WorkspaceViewRequestedCommand>? item = await View(queue, TimeSpan.FromMilliseconds(250));
            if (item is not null && string.Equals(item.Context.IdempotencyKey, key, StringComparison.Ordinal))
            {
                return item;
            }
        }
        throw new InvalidOperationException($"Workspace view is missing after '{step}'");
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

    private static int Year(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("year").GetInt32();
    }

    private static int Month(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("month").GetInt32();
    }

    private static int CurrencyCount(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("currencies").GetArrayLength();
    }

    private static string Currency(string data, int index)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("currencies")[index].GetProperty("currency").GetString() ?? string.Empty;
    }

    private static decimal Total(string data, string currency, string name)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("summary").GetProperty("currencies").EnumerateArray())
        {
            if (string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                return decimal.Parse(node.GetProperty(name).GetRawText(), CultureInfo.InvariantCulture);
            }
        }
        throw new InvalidOperationException($"Workspace summary is missing currency '{currency}'");
    }

    private static decimal AccountTotal(string data, string currency, string account, string name)
    {
        using var item = JsonDocument.Parse(data);
        foreach (JsonElement node in item.RootElement.GetProperty("summary").GetProperty("currencies").EnumerateArray())
        {
            if (!string.Equals(node.GetProperty("currency").GetString(), currency, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (JsonElement current in node.GetProperty("accounts").EnumerateArray())
            {
                if (string.Equals(current.GetProperty("name").GetString(), account, StringComparison.Ordinal))
                {
                    return decimal.Parse(current.GetProperty(name).GetRawText(), CultureInfo.InvariantCulture);
                }
            }
        }
        throw new InvalidOperationException($"Workspace summary is missing account '{account}' for '{currency}'");
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
            Amount = amount;
            Category = category;
            Id = id;
            When = when;
        }
        public string Kind { get; }
        public string AccountName { get; }
        public string Amount { get; }
        public string Category { get; }
        public string Id { get; }
        public DateTimeOffset When { get; }
    }
}
