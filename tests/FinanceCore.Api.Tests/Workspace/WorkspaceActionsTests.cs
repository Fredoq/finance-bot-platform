using FinanceCore.Domain.Workspace.Policies;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace action policy behavior.
/// </summary>
public sealed class WorkspaceActionsTests
{
    /// <summary>
    /// Verifies that home state returns the add-account action.
    /// </summary>
    [Fact(DisplayName = "Returns codes for home state")]
    public void Returns_codes_for_home()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> code = item.Codes("home", false);
        Assert.Equal(["account.add"], code);
    }
    /// <summary>
    /// Verifies that account currency state returns codes for both custom modes.
    /// </summary>
    [Fact(DisplayName = "Returns codes for account currency state with and without custom mode")]
    public void Returns_codes_for_currency()
    {
        var item = new WorkspaceActions();
        IReadOnlyList<string> code = item.Codes("account.currency", false);
        IReadOnlyList<string> custom = item.Codes("account.currency", true);
        Assert.Equal(["account.currency.rub", "account.currency.usd", "account.currency.eur", "account.currency.other", "account.cancel"], code);
        Assert.Equal(["account.cancel"], custom);
    }
    /// <summary>
    /// Verifies that empty states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty workspace states")]
    public void Rejects_empty_state()
    {
        var item = new WorkspaceActions();
        Assert.Throws<ArgumentException>(() => item.Codes(string.Empty, false));
    }
    /// <summary>
    /// Verifies that unsupported states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects unsupported workspace states")]
    public void Rejects_unknown_state()
    {
        var item = new WorkspaceActions();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => item.Codes("account.unknown", false));
        Assert.Contains("WorkspaceActions.Codes", error.Message, StringComparison.Ordinal);
    }
}
