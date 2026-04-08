namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceSummary
{
    private readonly WorkspaceBody body;

    internal WorkspaceSummary(WorkspaceBody body) => this.body = body ?? throw new ArgumentNullException(nameof(body));

    internal WorkspaceMove Open(WorkspaceData data, DateTimeOffset when, string timeZone) => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(when, timeZone)));

    internal WorkspaceMove Action(WorkspaceData data, string code, DateTimeOffset when) => code switch
    {
        WorkspaceBody.SummaryPrevious => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, -1))),
        WorkspaceBody.SummaryNext when WorkspaceBody.SummaryHasNext(data.Summary, when) => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, 1))),
        WorkspaceBody.SummaryBack => Move(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.ChooseActionPrompt)),
        _ => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, 0), new StatusData("Use the buttons to change the month or go back", string.Empty)))
    };

    internal WorkspaceMove Text(WorkspaceData data) => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, 0), new StatusData("Use the buttons to change the month or go back", string.Empty)));

    private static WorkspaceMove Move(string code, WorkspaceData data) => new(code, data);
}
