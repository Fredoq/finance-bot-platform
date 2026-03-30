namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record AccountData
{
    public AccountData()
    {
        Id = string.Empty;
        Name = string.Empty;
        Currency = string.Empty;
        Amount = 0m;
    }
    internal AccountData(string id, string name, string currency, decimal amount)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Amount = amount;
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
        Expense = new ExpenseData();
        Income = new IncomeData();
        Recent = new RecentData();
        Choices = new ChoicesData();
        Status = new StatusData();
    }
    internal WorkspaceData(IReadOnlyList<AccountData> accounts, FinancialData financial, ExpenseData expense, IncomeData income, RecentData recent, ChoicesData choices, StatusData status, bool custom)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        if (accounts.Any(item => item is null))
        {
            throw new ArgumentException("Workspace accounts cannot contain null items", nameof(accounts));
        }
        Accounts = Array.AsReadOnly(accounts.ToArray());
        Financial = financial ?? throw new ArgumentNullException(nameof(financial));
        Expense = expense ?? throw new ArgumentNullException(nameof(expense));
        Income = income ?? throw new ArgumentNullException(nameof(income));
        Recent = recent ?? throw new ArgumentNullException(nameof(recent));
        Choices = choices ?? throw new ArgumentNullException(nameof(choices));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Custom = custom;
    }
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public FinancialData Financial { get; init; }
    public ExpenseData Expense { get; init; }
    public IncomeData Income { get; init; }
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
    internal FinancialData(string name, string currency, decimal? amount)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Amount = amount;
    }
    public string Name { get; init; }
    public string Currency { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed record ExpenseData
{
    public ExpenseData()
    {
        Account = new PickData();
        Category = new PickData();
    }
    internal ExpenseData(PickData account, PickData category, decimal? amount)
    {
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
    }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed record IncomeData
{
    public IncomeData()
    {
        Account = new PickData();
        Category = new PickData();
    }
    internal IncomeData(PickData account, PickData category, decimal? amount)
    {
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
    }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal? Amount { get; init; }
}

internal sealed record RecentData
{
    public RecentData()
    {
        Items = Array.AsReadOnly<RecentItemData>([]);
        Selected = new RecentItemData();
    }
    internal RecentData(int page, bool hasPrevious, bool hasNext, IReadOnlyList<RecentItemData> items, RecentItemData selected)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Any(item => item is null))
        {
            throw new ArgumentException("Workspace recent items cannot contain null items", nameof(items));
        }
        Page = page;
        HasPrevious = hasPrevious;
        HasNext = hasNext;
        Items = Array.AsReadOnly(items.ToArray());
        Selected = selected ?? throw new ArgumentNullException(nameof(selected));
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
    internal RecentItemData(int slot, string id, string kind, PickData account, PickData category, decimal amount, string currency, DateTimeOffset occurredUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        OccurredUtc = occurredUtc;
        Slot = slot;
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

internal sealed record PickData
{
    public PickData()
    {
        Id = string.Empty;
        Name = string.Empty;
        Note = string.Empty;
    }
    internal PickData(string id, string name, string note)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Note = note ?? throw new ArgumentNullException(nameof(note));
    }
    public string Id { get; init; }
    public string Name { get; init; }
    public string Note { get; init; }
}

internal sealed record ChoicesData
{
    public ChoicesData()
    {
        Accounts = Array.AsReadOnly<OptionData>([]);
        Categories = Array.AsReadOnly<OptionData>([]);
    }
    internal ChoicesData(IReadOnlyList<OptionData> accounts, IReadOnlyList<OptionData> categories)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(categories);
        if (accounts.Any(item => item is null))
        {
            throw new ArgumentException("Workspace account choices cannot contain null items", nameof(accounts));
        }
        if (categories.Any(item => item is null))
        {
            throw new ArgumentException("Workspace category choices cannot contain null items", nameof(categories));
        }
        Accounts = Array.AsReadOnly(accounts.ToArray());
        Categories = Array.AsReadOnly(categories.ToArray());
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
    internal OptionData(int slot, string id, string name, string note)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slot);
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Note = note ?? throw new ArgumentNullException(nameof(note));
        Slot = slot;
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
    internal StatusData(string error, string notice)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
        Notice = notice ?? throw new ArgumentNullException(nameof(notice));
    }
    public string Error { get; init; }
    public string Notice { get; init; }
}
