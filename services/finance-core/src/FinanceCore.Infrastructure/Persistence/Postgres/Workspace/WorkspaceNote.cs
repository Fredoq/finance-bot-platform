using Finance.Application.Contracts.Entry;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record WorkspaceMove
{
    internal WorkspaceMove(string code, WorkspaceData body, AccountDraft? account, string category, TransactionNote? transaction, CorrectionNote? correction = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        AccountValue = account;
        CategoryEntry = category ?? throw new ArgumentNullException(nameof(category));
        RecordValue = transaction;
        CorrectValue = correction;
    }
    internal string Code { get; }
    internal WorkspaceData Body { get; }
    internal AccountDraft? AccountValue { get; }
    internal string CategoryEntry { get; }
    internal TransactionNote? RecordValue { get; }
    internal CorrectionNote? CorrectValue { get; }
}

internal sealed record AccountDraft
{
    internal AccountDraft(string title, string unit, decimal total)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Total = total;
    }
    internal string Title { get; }
    internal string Unit { get; }
    internal decimal Total { get; }
}

internal sealed record TransactionNote
{
    internal TransactionNote(string accountId, string categoryId, decimal total, string kind)
    {
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Total = total;
        TransactionKind = kind ?? throw new ArgumentNullException(nameof(kind));
    }
    internal string AccountId { get; }
    internal string CategoryId { get; }
    internal decimal Total { get; }
    internal string TransactionKind { get; }
}

internal sealed record CorrectionNote
{
    internal CorrectionNote(string transactionId, string kind, string mode, string categoryId)
    {
        TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
        TransactionKind = kind switch
        {
            WorkspaceBody.ExpenseKind => kind,
            WorkspaceBody.IncomeKind => kind,
            _ => throw new ArgumentException("Workspace transaction kind is not supported", nameof(kind))
        };
        Mode = mode ?? throw new ArgumentNullException(nameof(mode));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
    }
    internal string TransactionId { get; }
    internal string TransactionKind { get; }
    internal string Mode { get; }
    internal string CategoryId { get; }
}

internal sealed record WorkspaceFrame
{
    internal WorkspaceFrame(Guid user, string room, string state, string body, string entry, string last, DateTimeOffset when)
    {
        UserValue = user;
        Room = room ?? throw new ArgumentNullException(nameof(room));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        Last = last ?? throw new ArgumentNullException(nameof(last));
        When = when;
    }
    internal Guid UserValue { get; }
    internal string Room { get; }
    internal string State { get; }
    internal string Body { get; }
    internal string Entry { get; }
    internal string Last { get; }
    internal DateTimeOffset When { get; }
}

internal sealed record WorkspaceMark
{
    internal WorkspaceMark(Guid id, long revision, WorkspaceFrame frame)
    {
        IdValue = id;
        Revision = revision;
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
    }
    internal Guid IdValue { get; }
    internal long Revision { get; }
    internal WorkspaceFrame Frame { get; }
}

internal sealed record WorkspaceWrite
{
    internal WorkspaceWrite(WorkspaceItem item, bool isNew)
    {
        State = item ?? throw new ArgumentNullException(nameof(item));
        IsNew = isNew;
    }
    internal WorkspaceItem State { get; }
    internal bool IsNew { get; }
}

internal sealed record WorkspaceViewNote
{
    internal WorkspaceViewNote(WorkspaceIdentity identity, WorkspaceProfile profile, bool isNewUser, bool isNewWorkspace, DateTimeOffset when)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        IsNewUser = isNewUser;
        IsNewWorkspace = isNewWorkspace;
        When = when;
    }
    internal WorkspaceIdentity Identity { get; }
    internal WorkspaceProfile Profile { get; }
    internal bool IsNewUser { get; }
    internal bool IsNewWorkspace { get; }
    internal DateTimeOffset When { get; }
}
