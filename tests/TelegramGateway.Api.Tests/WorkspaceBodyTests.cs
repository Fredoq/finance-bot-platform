using TelegramGateway.Application.Entry.Workspace.Slices;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace body parsing and state validation.
/// </summary>
public sealed class WorkspaceBodyTests
{
    /// <summary>
    /// Verifies that valid confirm state data is accepted.
    /// </summary>
    [Fact(DisplayName = "Parses confirm state data when all required account fields exist")]
    public void Parses_confirm_state()
    {
        WorkspaceData data = new WorkspaceBody().Data("account.confirm", "{\"accounts\":[],\"financial\":{\"name\":\"Cash\",\"currency\":\"USD\",\"amount\":12.5},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}");
        Assert.Equal("Cash", data.Financial.Name);
    }

    /// <summary>
    /// Verifies that recent category state requires category choices.
    /// </summary>
    [Fact(DisplayName = "Rejects recent category state when category choices are missing")]
    public void Rejects_recent_category_without_choices()
    {
        var body = new WorkspaceBody();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => body.Data("transaction.recent.category", "{\"accounts\":[],\"recent\":{\"page\":0,\"hasPrevious\":false,\"hasNext\":false,\"items\":[],\"selected\":{\"slot\":1,\"id\":\"t1\",\"kind\":\"expense\",\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"},\"amount\":10,\"currency\":\"USD\",\"occurredUtc\":\"2026-03-29T20:28:00+00:00\"}},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("requires category choices", error.Message, StringComparison.Ordinal);
    }
}
