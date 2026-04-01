namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceBreakdown
{
    private readonly WorkspaceBody body;

    internal WorkspaceBreakdown(WorkspaceBody body) => this.body = body ?? throw new ArgumentNullException(nameof(body));

    internal WorkspaceMove Open(WorkspaceData data) => Move(WorkspaceBody.BreakdownState, body.Breakdown(data, new BreakdownData(data.Summary.Year, data.Summary.Month, [])));

    internal WorkspaceMove Action(WorkspaceData data, string code, DateTimeOffset when) => code switch
    {
        WorkspaceBody.BreakdownPrevious => Move(WorkspaceBody.BreakdownState, body.Breakdown(data, WorkspaceBody.Month(data.Breakdown, -1))),
        WorkspaceBody.BreakdownNext when WorkspaceBody.BreakdownHasNext(data.Breakdown, when) => Move(WorkspaceBody.BreakdownState, body.Breakdown(data, WorkspaceBody.Month(data.Breakdown, 1))),
        WorkspaceBody.BreakdownBack => Move(WorkspaceBody.SummaryState, body.Summary(data, new SummaryData(data.Breakdown.Year, data.Breakdown.Month, []))),
        _ => Move(WorkspaceBody.BreakdownState, body.Breakdown(data, WorkspaceBody.Month(data.Breakdown, 0), new StatusData("Use the buttons to change the month or go back", string.Empty)))
    };

    internal WorkspaceMove Text(WorkspaceData data) => Move(WorkspaceBody.BreakdownState, body.Breakdown(data, WorkspaceBody.Month(data.Breakdown, 0), new StatusData("Use the buttons to change the month or go back", string.Empty)));

    private static WorkspaceMove Move(string code, WorkspaceData data) => new(code, data);
}
