namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceSummary
{
    private readonly WorkspaceBody body;

    internal WorkspaceSummary(WorkspaceBody body) => this.body = body ?? throw new ArgumentNullException(nameof(body));

    internal WorkspaceMove Open(WorkspaceData data, DateTimeOffset when) => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(when)));

    internal WorkspaceMove Action(WorkspaceData data, string code, DateTimeOffset when) => code switch
    {
        WorkspaceBody.SummaryPrevious => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, -1))),
        WorkspaceBody.SummaryNext when CanNext(data, when) => Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(data.Summary, 1))),
        WorkspaceBody.SummaryBack => Move(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.ChooseActionPrompt)),
        _ => Move(WorkspaceBody.SummaryState, body.Summary(data, data.Summary, new StatusData("Use the buttons to change the month or go back", string.Empty)))
    };

    internal WorkspaceMove Text(WorkspaceData data) => Move(WorkspaceBody.SummaryState, body.Summary(data, data.Summary, new StatusData("Use the buttons to change the month or go back", string.Empty)));

    private static WorkspaceMove Move(string code, WorkspaceData data) => new(code, data);

    private static bool CanNext(WorkspaceData data, DateTimeOffset when)
    {
        if (data.Summary.Year <= 0 || data.Summary.Month <= 0)
        {
            return false;
        }
        DateTimeOffset current = new(when.Year, when.Month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset selected = new(data.Summary.Year, data.Summary.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return selected < current;
    }
}
