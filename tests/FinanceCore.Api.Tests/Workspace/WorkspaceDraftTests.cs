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
        WorkspaceMove move = item.Home(new WorkspaceData(), WorkspaceBody.AddExpense);
        Assert.Equal(WorkspaceBody.HomeState, move.Code);
    }
}
