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
    public void Preserves_state_for_unknown_kind()
    {
        var body = new WorkspaceBody();
        var draft = new WorkspaceDraft(body, new WorkspaceAmount());
        var recent = new WorkspaceRecent(body);
        var item = new WorkspaceInput(body, draft, recent);
        WorkspaceMove move = item.Move(WorkspaceBody.HomeState, body.Home([], "notice"), new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "voice", "hello", DateTimeOffset.UtcNow));
        Assert.Equal(WorkspaceBody.HomeState, move.Code);
    }

    /// <summary>
    /// Verifies that account cancel returns to home.
    /// </summary>
    [Fact(DisplayName = "Returns home after account cancel action")]
    public void Cancels_account()
    {
        var body = new WorkspaceBody();
        var draft = new WorkspaceDraft(body, new WorkspaceAmount());
        var recent = new WorkspaceRecent(body);
        var item = new WorkspaceInput(body, draft, recent);
        WorkspaceMove move = item.Move(WorkspaceBody.NameState, body.Account(new WorkspaceData(), new FinancialData()), new WorkspaceInputRequestedCommand(new WorkspaceIdentity("actor", "room"), new WorkspaceProfile("Alex", "en"), "action", WorkspaceBody.AccountCancel, DateTimeOffset.UtcNow));
        Assert.Equal(WorkspaceBody.HomeState, move.Code);
    }
}
