using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers account and transaction draft transitions.
/// </summary>
public sealed class WorkspaceDraftTests
{
    /// <summary>
    /// Verifies that empty account name is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty account names during onboarding")]
    public void Rejects_name()
    {
        var body = new WorkspaceBody();
        var item = new WorkspaceDraft(body, new WorkspaceAmount());
        WorkspaceMove move = item.Name(body.Account(new WorkspaceData(), new FinancialData(string.Empty, string.Empty, null)), " ");
        Assert.Equal(WorkspaceBody.NameState, move.Code);
    }

    /// <summary>
    /// Verifies that a missing account list prevents transaction start.
    /// </summary>
    [Fact(DisplayName = "Returns home when expense flow starts without accounts")]
    public void Rejects_expense_without_accounts()
    {
        var body = new WorkspaceBody();
        var item = new WorkspaceDraft(body, new WorkspaceAmount());
        WorkspaceMove move = item.Home(new WorkspaceData(), WorkspaceBody.AddExpense, DateTimeOffset.UtcNow, WorkspaceZone.Default);
        Assert.Equal(WorkspaceBody.HomeState, move.Code);
    }

    /// <summary>
    /// Verifies that currency codes accept ASCII letters only.
    /// </summary>
    [Fact(DisplayName = "Rejects non ASCII currency codes")]
    public void Rejects_currency()
    {
        WorkspaceBody body = new();
        WorkspaceDraft item = new(body, new WorkspaceAmount());
        WorkspaceData data = body.Account(new WorkspaceData(), new FinancialData("Cash", string.Empty, null), custom: true);
        WorkspaceMove move = item.Code(data, "руб");
        Assert.Equal(WorkspaceBody.CurrencyState, move.Code);
    }

    /// <summary>
    /// Verifies that account names fail fast on null input.
    /// </summary>
    [Fact(DisplayName = "Rejects null account name input")]
    public void Rejects_name_null()
    {
        WorkspaceBody body = new();
        WorkspaceDraft item = new(body, new WorkspaceAmount());
        string value = null!;
        Assert.Throws<ArgumentNullException>(() => item.Name(new WorkspaceData(), value));
    }

    /// <summary>
    /// Verifies that currency code input fails fast on null input.
    /// </summary>
    [Fact(DisplayName = "Rejects null currency code input")]
    public void Rejects_code_null()
    {
        WorkspaceBody body = new();
        WorkspaceDraft item = new(body, new WorkspaceAmount());
        string value = null!;
        Assert.Throws<ArgumentNullException>(() => item.Code(new WorkspaceData(), value));
    }

    /// <summary>
    /// Verifies that category text fails fast on null input.
    /// </summary>
    [Fact(DisplayName = "Rejects null category text input")]
    public void Rejects_text_null()
    {
        WorkspaceBody body = new();
        WorkspaceDraft item = new(body, new WorkspaceAmount());
        string value = null!;
        Assert.Throws<ArgumentNullException>(() => item.Text(new WorkspaceData(), value, false));
    }
}
