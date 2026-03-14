namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record WorkspaceItem(Guid Id, string State, string Data, long Revision, bool IsNew);
