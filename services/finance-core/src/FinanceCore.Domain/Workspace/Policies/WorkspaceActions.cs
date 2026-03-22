namespace FinanceCore.Domain.Workspace.Policies;

/// <summary>
/// Resolves the baseline action codes for a workspace state.
/// </summary>
public sealed class WorkspaceActions : IWorkspaceActions
{
    private const string Cancel = "account.cancel";
    /// <summary>
    /// Gets the supported action codes for the workspace state.
    /// </summary>
    /// <param name="state">The workspace state code.</param>
    /// <param name="custom">Indicates whether the state expects custom currency input.</param>
    /// <returns>The supported action codes.</returns>
    public IReadOnlyList<string> Codes(string state, bool custom) => state switch
    {
        "home" => ["account.add"],
        "account.name" => [Cancel],
        "account.currency" when custom => [Cancel],
        "account.currency" => ["account.currency.rub", "account.currency.usd", "account.currency.eur", "account.currency.other", Cancel],
        "account.balance" => [Cancel],
        "account.confirm" => ["account.create", Cancel],
        _ => [Cancel]
    };
}
