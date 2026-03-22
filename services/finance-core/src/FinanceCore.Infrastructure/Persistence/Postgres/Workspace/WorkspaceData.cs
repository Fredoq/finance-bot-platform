namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record AccountData
{
    public AccountData()
    {
        Name = string.Empty;
        Currency = string.Empty;
        Amount = 0m;
    }
    internal AccountData(string name, string currency, decimal amount)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Amount = amount;
    }
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
        Status = new StatusData();
    }
    internal WorkspaceData(IReadOnlyList<AccountData> accounts, FinancialData financial, StatusData status, bool custom)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        if (accounts.Any(item => item is null))
        {
            throw new ArgumentException("Workspace accounts cannot contain null items", nameof(accounts));
        }
        Accounts = Array.AsReadOnly(accounts.ToArray());
        Financial = financial ?? throw new ArgumentNullException(nameof(financial));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Custom = custom;
    }
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public FinancialData Financial { get; init; }
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
