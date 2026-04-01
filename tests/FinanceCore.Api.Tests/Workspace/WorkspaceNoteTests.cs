using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace note validation.
/// </summary>
public sealed class WorkspaceNoteTests
{
    /// <summary>
    /// Verifies that correction notes reject unsupported transaction kinds.
    /// </summary>
    [Fact(DisplayName = "Rejects unsupported transaction kinds in correction notes")]
    public void Reject()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new CorrectionNote(Guid.CreateVersion7().ToString(), "transfer", WorkspaceBody.RecategorizeMode, string.Empty));
        Assert.Equal("kind", error.ParamName);
    }
}
