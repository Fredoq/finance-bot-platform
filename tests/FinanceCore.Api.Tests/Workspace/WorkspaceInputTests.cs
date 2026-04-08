using Finance.Application.Contracts.Entry;
using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace input routing across action and text kinds.
/// </summary>
public sealed class WorkspaceInputTests
{
    /// <summary>
    /// Verifies that unsupported input kinds preserve the current state code.
    /// </summary>
    [Fact(DisplayName = "Preserves the current state for unsupported input kinds")]
    public void Preserve()
    {
        WorkspaceBody body = new();
        WorkspaceDraft draft = new(body, new WorkspaceAmount());
        WorkspaceRecent recent = new(body);
        WorkspaceInput item = new(body, draft, recent, new WorkspaceSummary(body), new WorkspaceBreakdown(body));
        const string state = WorkspaceBody.ExpenseAmountState;
        WorkspaceMove move = item.Move(state, body.Transaction(new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData()), new PickData("a1", "Cash", "USD"), new PickData(), null, false), new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "voice", "hello", DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, WorkspaceZone.Default);
        Assert.Equal(state, move.Code);
    }

    /// <summary>
    /// Verifies that account cancel returns to home.
    /// </summary>
    [Fact(DisplayName = "Returns home after account cancel action")]
    public void Cancel()
    {
        WorkspaceBody body = new();
        WorkspaceDraft draft = new(body, new WorkspaceAmount());
        WorkspaceRecent recent = new(body);
        WorkspaceInput item = new(body, draft, recent, new WorkspaceSummary(body), new WorkspaceBreakdown(body));
        WorkspaceMove move = item.Move(WorkspaceBody.NameState, body.Account(new WorkspaceData(), new FinancialData()), new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "action", WorkspaceBody.AccountCancel, DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, WorkspaceZone.Default);
        Assert.Equal(WorkspaceBody.HomeState, move.Code);
    }

    /// <summary>
    /// Verifies that legacy expense confirm snapshots without source return to the source step.
    /// </summary>
    [Fact(DisplayName = "Routes a legacy expense confirm snapshot without source back to source")]
    public void Expense_source()
    {
        WorkspaceBody body = new();
        WorkspaceDraft draft = new(body, new WorkspaceAmount());
        WorkspaceRecent recent = new(body);
        WorkspaceInput item = new(body, draft, recent, new WorkspaceSummary(body), new WorkspaceBreakdown(body));
        WorkspaceData data = body.Transaction(new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData()), new PickData("a1", "Cash", "USD"), new PickData("c1", "Food", "food"), 5m, false);
        WorkspaceMove move = item.Move(WorkspaceBody.ExpenseConfirmState, data, new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "text", "hello", DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, WorkspaceZone.Default);
        Assert.Equal(WorkspaceBody.ExpenseSourceState, move.Code);
    }

    /// <summary>
    /// Verifies that legacy income confirm snapshots without source return to the source step.
    /// </summary>
    [Fact(DisplayName = "Routes a legacy income confirm snapshot without source back to source")]
    public void Income_source()
    {
        WorkspaceBody body = new();
        WorkspaceDraft draft = new(body, new WorkspaceAmount());
        WorkspaceRecent recent = new(body);
        WorkspaceInput item = new(body, draft, recent, new WorkspaceSummary(body), new WorkspaceBreakdown(body));
        WorkspaceData data = body.Transaction(new WorkspaceData([new AccountData("a1", "Cash", "USD", 10m)], new WorkspaceStateData()), new PickData("a1", "Cash", "USD"), new PickData("c1", "Salary", "salary"), 5m, true);
        WorkspaceMove move = item.Move(WorkspaceBody.IncomeConfirmState, data, new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "text", "hello", DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, WorkspaceZone.Default);
        Assert.Equal(WorkspaceBody.IncomeSourceState, move.Code);
    }
}
