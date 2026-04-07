using System.Text.Json;
using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Entry.Workspace.Slices;

namespace TelegramGateway.Api.Tests;

internal static class WorkspaceStateNote
{
    public static WorkspaceViewRequestedCommand View(string state, WorkspaceData data, params string[] actions)
        => new(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), new WorkspaceViewFrame(state, JsonSerializer.Serialize(data), actions), new WorkspaceViewFreshness(false, false), DateTimeOffset.UtcNow);
    public static WorkspaceData RecentList(int page, bool hasPrevious, bool hasNext, IReadOnlyList<RecentItemData> items, string notice = "")
        => new()
        {
            Accounts = [Account()],
            Recent = new RecentData { Page = page, HasPrevious = hasPrevious, HasNext = hasNext, Items = items, Selected = new RecentItemData() },
            Choices = new ChoicesData(),
            Status = new StatusData { Error = string.Empty, Notice = notice }
        };
    public static WorkspaceData RecentDetail(RecentItemData selected)
        => new()
        {
            Accounts = [Account()],
            Recent = new RecentData { Page = 0, HasPrevious = false, HasNext = false, Items = [], Selected = selected },
            Choices = new ChoicesData(),
            Status = new StatusData()
        };
    public static WorkspaceData Summary(int year, int month, IReadOnlyList<SummaryCurrencyData> currencies, string notice = "", string timeZone = "Etc/UTC")
        => new()
        {
            Accounts = [Account()],
            Summary = new SummaryData { Year = year, Month = month, TimeZone = timeZone, Currencies = currencies },
            Choices = new ChoicesData(),
            Status = new StatusData { Error = string.Empty, Notice = notice }
        };
    public static WorkspaceData Breakdown(int year, int month, IReadOnlyList<BreakdownCurrencyData> currencies, string notice = "", string timeZone = "Etc/UTC")
        => new()
        {
            Accounts = [Account()],
            Breakdown = new BreakdownData { Year = year, Month = month, TimeZone = timeZone, Currencies = currencies },
            Choices = new ChoicesData(),
            Status = new StatusData { Error = string.Empty, Notice = notice }
        };
    public static SummaryCurrencyData Currency(string currency, decimal income, decimal expense, params SummaryAccountData[] accounts)
        => new()
        {
            Currency = currency,
            Income = income,
            Expense = expense,
            Net = income - expense,
            Accounts = accounts
        };
    public static BreakdownCurrencyData BreakdownCurrency(string currency, decimal total, params BreakdownCategoryData[] categories)
        => new()
        {
            Currency = currency,
            Total = total,
            Categories = categories
        };
    public static BreakdownCategoryData BreakdownCategory(string name, string code, decimal amount, decimal share)
        => new()
        {
            Name = name,
            Code = code,
            Amount = amount,
            Share = share
        };
    public static SummaryAccountData Account(string id, string name, decimal income, decimal expense)
        => new()
        {
            Id = id,
            Name = name,
            Income = income,
            Expense = expense,
            Net = income - expense
        };
    public static RecentItemData RecentItem(int slot, string id, string kind, string category, string code, decimal amount, DateTimeOffset occurred)
        => new()
        {
            Slot = slot,
            Id = id,
            Kind = kind,
            Account = new PickData { Id = "a1", Name = "Cash", Note = "USD" },
            Category = new PickData { Id = $"c{slot}", Name = category, Note = code },
            Amount = amount,
            Currency = "USD",
            OccurredUtc = occurred
        };
    private static AccountData Account() => new() { Id = "a1", Name = "Cash", Currency = "USD", Amount = 1200m };
}
