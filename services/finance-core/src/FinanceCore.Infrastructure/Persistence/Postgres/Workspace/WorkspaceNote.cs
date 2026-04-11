using Finance.Application.Contracts.Entry;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed record WorkspaceMove
{
    internal WorkspaceMove(string code, WorkspaceData body) : this(code, body, null, string.Empty, null)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, AccountDraft account) : this(code, body, account, string.Empty, null)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, string text) : this(code, body, null, text, null)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, TransactionNote transaction) : this(code, body, null, string.Empty, transaction)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, TimeZoneNote zone) : this(code, body, null, string.Empty, null, null, zone)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, TransferNote transfer)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        AccountValue = null;
        TextEntry = string.Empty;
        RecordValue = null;
        CorrectValue = null;
        TimeZoneValue = null;
        TransferValue = transfer ?? throw new ArgumentNullException(nameof(transfer));
    }

    internal WorkspaceMove(string code, WorkspaceData body, CorrectionNote correction) : this(code, body, null, string.Empty, null, correction)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, string category, CorrectionNote correction) : this(code, body, null, category, null, correction)
    {
    }

    internal WorkspaceMove(string code, WorkspaceData body, AccountDraft? account, string text, TransactionNote? transaction, CorrectionNote? correction = null, TimeZoneNote? zone = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        AccountValue = account;
        TextEntry = text ?? throw new ArgumentNullException(nameof(text));
        RecordValue = transaction;
        CorrectValue = correction;
        TimeZoneValue = zone;
        TransferValue = null;
    }
    internal string Code { get; }
    internal WorkspaceData Body { get; }
    internal AccountDraft? AccountValue { get; }
    internal string TextEntry { get; }
    internal TransactionNote? RecordValue { get; }
    internal CorrectionNote? CorrectValue { get; }
    internal TimeZoneNote? TimeZoneValue { get; }
    internal TransferNote? TransferValue { get; }
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
    internal TransactionNote(string accountId, string categoryId, decimal total, string kind, string sourceText)
    {
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Total = total;
        TransactionKind = kind ?? throw new ArgumentNullException(nameof(kind));
        SourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
    }
    internal string AccountId { get; }
    internal string CategoryId { get; }
    internal decimal Total { get; }
    internal string TransactionKind { get; }
    internal string SourceText { get; }
}

internal sealed record TransferNote
{
    internal TransferNote(string sourceId, string targetId, string currency, decimal total)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Total = total;
    }
    internal string SourceId { get; }
    internal string TargetId { get; }
    internal string Currency { get; }
    internal decimal Total { get; }
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

internal sealed record TimeZoneNote
{
    internal TimeZoneNote(string zoneId)
    {
        ArgumentNullException.ThrowIfNull(zoneId);
        if (!WorkspaceZone.Try(zoneId, out string value))
        {
            throw new ArgumentException("Workspace time zone is not supported", nameof(zoneId));
        }
        ZoneId = value;
    }
    internal string ZoneId { get; }
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
