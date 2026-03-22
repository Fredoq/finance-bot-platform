using FinanceCore.Domain.Workspace.Policies;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace action policy behavior.
/// </summary>
public sealed class WorkspaceActionsTests
{
    /// <summary>
    /// Verifies that empty states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects empty workspace states")]
    public void Rejects_empty_state()
    {
        var item = new WorkspaceActions();
        Assert.Throws<ArgumentException>(() => item.Codes(string.Empty, false));
    }
    /// <summary>
    /// Verifies that unsupported states are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects unsupported workspace states")]
    public void Rejects_unknown_state()
    {
        var item = new WorkspaceActions();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => item.Codes("account.unknown", false));
        Assert.Contains("WorkspaceActions.Codes", error.Message, StringComparison.Ordinal);
    }
}
