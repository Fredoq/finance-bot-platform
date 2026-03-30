namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed record AccountData
{
    public AccountData()
    {
        Id = string.Empty;
        Name = string.Empty;
        Currency = string.Empty;
        Amount = 0m;
    }
    public string Id { get; init; }
    public string Name { get; init; }
    public string Currency { get; init; }
    public decimal Amount { get; init; }
}

internal sealed record WorkspaceData
{
    public WorkspaceData()
    {
        Accounts = Array.AsReadOnly<AccountData>([]);
        Financial = new FinancialData();
        Expense = new TransactionData();
        Income = new TransactionData();
        Recent = new RecentData();
        Choices = new ChoicesData();
        Status = new StatusData();
    }
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public FinancialData Financial { get; init; }
    public TransactionData Expense { get; init; }
    public TransactionData Income { get; init; }
    public RecentData Recent { get; init; }
    public ChoicesData Choices { get; init; }
    public StatusData Status { get; init; }
    public bool Custom { get; init; }
}

internal sealed record FinancialData
{
    public FinancialData()
    {
        Name = string.Empty;
        Currency = string.Empty;
    }
    public string Name { get; init; }
    public string Currency { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed record TransactionData
{
    public TransactionData()
    {
        Account = new PickData();
        Category = new PickData();
    }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed record PickData
{
    public PickData()
    {
        Id = string.Empty;
        Name = string.Empty;
        Note = string.Empty;
    }
    public string Id { get; init; }
    public string Name { get; init; }
    public string Note { get; init; }
}

internal sealed record RecentData
{
    public RecentData()
    {
        Items = Array.AsReadOnly<RecentItemData>([]);
        Selected = new RecentItemData();
    }
    public int Page { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }
    public IReadOnlyList<RecentItemData> Items { get; init; }
    public RecentItemData Selected { get; init; }
}

internal sealed record RecentItemData
{
    public RecentItemData()
    {
        Id = string.Empty;
        Kind = string.Empty;
        Account = new PickData();
        Category = new PickData();
        Currency = string.Empty;
    }
    public int Slot { get; init; }
    public string Id { get; init; }
    public string Kind { get; init; }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTimeOffset OccurredUtc { get; init; }
}

internal sealed record ChoicesData
{
    public ChoicesData()
    {
        Accounts = Array.AsReadOnly<OptionData>([]);
        Categories = Array.AsReadOnly<OptionData>([]);
    }
    public IReadOnlyList<OptionData> Accounts { get; init; }
    public IReadOnlyList<OptionData> Categories { get; init; }
}

internal sealed record OptionData
{
    public OptionData()
    {
        Id = string.Empty;
        Name = string.Empty;
        Note = string.Empty;
    }
    public int Slot { get; init; }
    public string Id { get; init; }
    public string Name { get; init; }
    public string Note { get; init; }
}

internal sealed record StatusData
{
    public StatusData()
    {
        Error = string.Empty;
        Notice = string.Empty;
    }
    public string Error { get; init; }
    public string Notice { get; init; }
}
