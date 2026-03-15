namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record WorkspaceSnapshot
{
    internal WorkspaceSnapshot(string state, string data, long revision, bool isNew)
    {
        State = !string.IsNullOrWhiteSpace(state) ? state : throw new ArgumentException("Workspace state is required", nameof(state));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Revision = revision > 0 ? revision : throw new ArgumentOutOfRangeException(nameof(revision));
        IsNew = isNew;
    }
    internal string State { get; }
    internal string Data { get; }
    internal long Revision { get; }
    internal bool IsNew { get; }
}

internal sealed record WorkspaceItem
{
    internal WorkspaceItem(Guid id, WorkspaceSnapshot snapshot)
    {
        Id = id;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }
    internal Guid Id { get; }
    internal WorkspaceSnapshot Snapshot { get; }
}
