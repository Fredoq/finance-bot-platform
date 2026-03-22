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
        Name = string.Empty;
        Currency = string.Empty;
        Error = string.Empty;
        Notice = string.Empty;
    }
    internal WorkspaceData(IReadOnlyList<AccountData> accounts, string name, string currency, decimal? amount, string error, string notice, bool custom)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        Accounts = Array.AsReadOnly(accounts.Where(item => item is not null).ToArray());
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Amount = amount;
        Error = error ?? throw new ArgumentNullException(nameof(error));
        Notice = notice ?? throw new ArgumentNullException(nameof(notice));
        Custom = custom;
    }
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public string Name { get; init; }
    public string Currency { get; init; }
    public decimal? Amount { get; init; }
    public string Error { get; init; }
    public string Notice { get; init; }
    public bool Custom { get; init; }
}
