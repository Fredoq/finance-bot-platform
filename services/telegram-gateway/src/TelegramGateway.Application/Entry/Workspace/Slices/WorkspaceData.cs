namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed record AccountData
{
    public AccountData()
    {
        Name = string.Empty;
        Currency = string.Empty;
        Amount = 0m;
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
    public IReadOnlyList<AccountData> Accounts { get; init; }
    public string Name { get; init; }
    public string Currency { get; init; }
    public decimal? Amount { get; init; }
    public string Error { get; init; }
    public string Notice { get; init; }
    public bool Custom { get; init; }
}
