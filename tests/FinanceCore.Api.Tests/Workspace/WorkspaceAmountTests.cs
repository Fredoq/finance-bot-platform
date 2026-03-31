using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers amount parsing and scale rules for workspace input.
/// </summary>
public sealed class WorkspaceAmountTests
{
    /// <summary>
    /// Verifies that comma decimal input is parsed successfully.
    /// </summary>
    [Fact(DisplayName = "Parses comma decimal values")]
    public void Parses_amount()
    {
        bool ok = new WorkspaceAmount().Try("12,5", out decimal value);
        Assert.True(ok);
        Assert.Equal(12.5m, value);
    }

    /// <summary>
    /// Verifies that decimal scale is measured correctly.
    /// </summary>
    [Fact(DisplayName = "Measures decimal scale")]
    public void Measures_scale()
    {
        int value = new WorkspaceAmount().Scale(12.3456m);
        Assert.Equal(4, value);
    }
}
