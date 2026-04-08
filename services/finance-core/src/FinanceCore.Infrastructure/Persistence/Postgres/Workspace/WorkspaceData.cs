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
        Summary = new SummaryData();
        Breakdown = new BreakdownData();
        Choices = new ChoicesData();
        Status = new StatusData();
    }
    internal WorkspaceData(IReadOnlyList<AccountData> accounts, WorkspaceStateData state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(accounts);
        if (accounts.Any(item => item is null))
        {
            throw new ArgumentException("Workspace accounts cannot contain null items", nameof(accounts));
        }
        Accounts = Array.AsReadOnly(accounts.ToArray());
        Financial = state.Financial;
        Expense = state.Expense;
        Income = state.Income;
        Recent = state.Recent;
        Summary = state.Summary;
        Breakdown = state.Breakdown ?? throw new ArgumentNullException("state.Breakdown");
        Choices = state.Choices;
        Status = state.Status;
        Custom = state.Custom;
    }
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public FinancialData Financial { get; init; }
    public ExpenseData Expense { get; init; }
    public IncomeData Income { get; init; }
    public RecentData Recent { get; init; }
    public SummaryData Summary { get; init; }
    public BreakdownData Breakdown { get; init; }
    public ChoicesData Choices { get; init; }
    public StatusData Status { get; init; }
    public bool Custom { get; init; }
}

internal sealed record WorkspaceStateData
{
    public WorkspaceStateData()
    {
        Financial = new FinancialData();
        Expense = new ExpenseData();
        Income = new IncomeData();
        Recent = new RecentData();
        Summary = new SummaryData();
        Breakdown = new BreakdownData();
        Choices = new ChoicesData();
        Status = new StatusData();
    }
    public FinancialData Financial { get; init; }
    public ExpenseData Expense { get; init; }
    public IncomeData Income { get; init; }
    public RecentData Recent { get; init; }
    public SummaryData Summary { get; init; }
    public BreakdownData Breakdown { get; init; }
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
        Source = string.Empty;
    }
    internal ExpenseData(PickData account, PickData category, decimal? amount, string source)
    {
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal? Amount { get; init; }
    public string Source { get; init; }
}

internal sealed record IncomeData
{
    public IncomeData()
    {
        Account = new PickData();
        Category = new PickData();
        Source = string.Empty;
    }
    internal IncomeData(PickData account, PickData category, decimal? amount, string source)
    {
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal? Amount { get; init; }
    public string Source { get; init; }
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
        Source = string.Empty;
    }
    internal RecentItemData(int slot, RecentEntryData entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        Id = entry.Id;
        Kind = entry.Kind;
        Account = entry.Account;
        Category = entry.Category;
        Amount = entry.Amount;
        Currency = entry.Currency;
        OccurredUtc = entry.OccurredUtc;
        Source = entry.Source;
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
    public string Source { get; init; }
}

internal sealed record RecentEntryData
{
    public RecentEntryData()
    {
        Id = string.Empty;
        Kind = string.Empty;
        Account = new PickData();
        Category = new PickData();
        Currency = string.Empty;
        Source = string.Empty;
    }
    internal RecentEntryData(string id, string kind, PickData account, PickData category, decimal amount, string currency, DateTimeOffset occurredUtc)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        OccurredUtc = occurredUtc;
        Source = string.Empty;
    }
    public string Id { get; init; }
    public string Kind { get; init; }
    public PickData Account { get; init; }
    public PickData Category { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTimeOffset OccurredUtc { get; init; }
    public string Source { get; init; }
}

internal sealed record SummaryData
{
    public SummaryData()
    {
        TimeZone = WorkspaceZone.Default;
        Currencies = Array.AsReadOnly<SummaryCurrencyData>([]);
    }
    internal SummaryData(int year, int month, string timeZone, IReadOnlyList<SummaryCurrencyData> currencies)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new ArgumentException("Workspace summary time zone is required", nameof(timeZone));
        }
        ArgumentNullException.ThrowIfNull(currencies);
        if (currencies.Any(item => item is null))
        {
            throw new ArgumentException("Workspace summary currencies cannot contain null items", nameof(currencies));
        }
        Year = year;
        Month = month;
        TimeZone = timeZone;
        Currencies = Array.AsReadOnly(currencies.ToArray());
    }
    public int Year { get; init; }
    public int Month { get; init; }
    public string TimeZone { get; init; }
    public IReadOnlyList<SummaryCurrencyData> Currencies { get; init; }
}

internal sealed record SummaryCurrencyData
{
    public SummaryCurrencyData()
    {
        Currency = string.Empty;
        Accounts = Array.AsReadOnly<SummaryAccountData>([]);
    }
    internal SummaryCurrencyData(string currency, decimal income, decimal expense, decimal net, IReadOnlyList<SummaryAccountData> accounts)
    {
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        ArgumentNullException.ThrowIfNull(accounts);
        if (accounts.Any(item => item is null))
        {
            throw new ArgumentException("Workspace summary accounts cannot contain null items", nameof(accounts));
        }
        Income = income;
        Expense = expense;
        Net = net;
        Accounts = Array.AsReadOnly(accounts.ToArray());
    }
    public string Currency { get; init; }
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
    public decimal Net { get; init; }
    public IReadOnlyList<SummaryAccountData> Accounts { get; init; }
}

internal sealed record SummaryAccountData
{
    public SummaryAccountData()
    {
        Id = string.Empty;
        Name = string.Empty;
    }
    internal SummaryAccountData(string id, string name, decimal income, decimal expense, decimal net)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Income = income;
        Expense = expense;
        Net = net;
    }
    public string Id { get; init; }
    public string Name { get; init; }
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
    public decimal Net { get; init; }
}

internal sealed record BreakdownData
{
    public BreakdownData()
    {
        TimeZone = WorkspaceZone.Default;
        Currencies = Array.AsReadOnly<BreakdownCurrencyData>([]);
    }
    internal BreakdownData(int year, int month, string timeZone, IReadOnlyList<BreakdownCurrencyData> currencies)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new ArgumentException("Workspace breakdown time zone is required", nameof(timeZone));
        }
        ArgumentNullException.ThrowIfNull(currencies);
        if (currencies.Any(item => item is null))
        {
            throw new ArgumentException("Workspace breakdown currencies cannot contain null items", nameof(currencies));
        }
        Year = year;
        Month = month;
        TimeZone = timeZone;
        Currencies = Array.AsReadOnly(currencies.ToArray());
    }
    public int Year { get; init; }
    public int Month { get; init; }
    public string TimeZone { get; init; }
    public IReadOnlyList<BreakdownCurrencyData> Currencies { get; init; }
}

internal sealed record BreakdownCurrencyData
{
    public BreakdownCurrencyData()
    {
        Currency = string.Empty;
        Categories = Array.AsReadOnly<BreakdownCategoryData>([]);
    }
    internal BreakdownCurrencyData(string currency, decimal total, IReadOnlyList<BreakdownCategoryData> categories)
    {
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        ArgumentNullException.ThrowIfNull(categories);
        if (categories.Any(item => item is null))
        {
            throw new ArgumentException("Workspace breakdown categories cannot contain null items", nameof(categories));
        }
        Total = total;
        Categories = Array.AsReadOnly(categories.ToArray());
    }
    public string Currency { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<BreakdownCategoryData> Categories { get; init; }
}

internal sealed record BreakdownCategoryData
{
    public BreakdownCategoryData()
    {
        Name = string.Empty;
        Code = string.Empty;
    }
    internal BreakdownCategoryData(string name, string code, decimal amount, decimal share)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Amount = amount;
        Share = share;
    }
    public string Name { get; init; }
    public string Code { get; init; }
    public decimal Amount { get; init; }
    public decimal Share { get; init; }
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
