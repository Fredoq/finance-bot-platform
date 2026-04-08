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
    /// <summary>
    /// Verifies that category screens reject missing source text.
    /// </summary>
    [Fact(DisplayName = "Rejects expense category state when source is missing")]
    public void Rejects_expense_category_without_source()
    {
        WorkspaceBody body = new();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => body.Data("transaction.expense.category", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":12.5,\"source\":\"\"},\"choices\":{\"accounts\":[],\"categories\":[{\"slot\":1,\"id\":\"c1\",\"name\":\"Food\",\"note\":\"food\"}]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("requires source", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that source screens reject missing amounts with source-specific validation.
    /// </summary>
    [Fact(DisplayName = "Rejects source states when amount is missing")]
    public void Rejects_source_without_amount()
    {
        WorkspaceBody body = new();
        InvalidOperationException expense = Assert.Throws<InvalidOperationException>(() => body.Data("transaction.expense.source", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        InvalidOperationException income = Assert.Throws<InvalidOperationException>(() => body.Data("transaction.income.source", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"expense\":{\"account\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"income\":{\"account\":{\"id\":\"a1\",\"name\":\"Cash\",\"note\":\"USD\"},\"category\":{\"id\":\"\",\"name\":\"\",\"note\":\"\"},\"amount\":null,\"source\":\"\"},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("transaction.expense.source' requires amount", expense.Message, StringComparison.Ordinal);
        Assert.Contains("transaction.income.source' requires amount", income.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that summary state requires year and month.
    /// </summary>
    [Fact(DisplayName = "Rejects monthly summary state when year or month is missing")]
    public void Rejects_summary_without_period()
    {
        WorkspaceBody body = new();
        InvalidOperationException year = Assert.Throws<InvalidOperationException>(() => body.Data("summary.month", "{\"accounts\":[],\"summary\":{\"year\":0,\"month\":4,\"timeZone\":\"Etc/UTC\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        InvalidOperationException month = Assert.Throws<InvalidOperationException>(() => body.Data("summary.month", "{\"accounts\":[],\"summary\":{\"year\":2026,\"month\":0,\"timeZone\":\"Etc/UTC\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("requires year", year.Message, StringComparison.Ordinal);
        Assert.Contains("requires month", month.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that breakdown state requires year and month.
    /// </summary>
    [Fact(DisplayName = "Rejects category breakdown state when year or month is missing")]
    public void Rejects_breakdown_without_period()
    {
        WorkspaceBody body = new();
        InvalidOperationException year = Assert.Throws<InvalidOperationException>(() => body.Data("category.month", "{\"accounts\":[],\"breakdown\":{\"year\":0,\"month\":4,\"timeZone\":\"Etc/UTC\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        InvalidOperationException month = Assert.Throws<InvalidOperationException>(() => body.Data("category.month", "{\"accounts\":[],\"breakdown\":{\"year\":2026,\"month\":0,\"timeZone\":\"Etc/UTC\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("requires year", year.Message, StringComparison.Ordinal);
        Assert.Contains("requires month", month.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that report screens require a time zone label.
    /// </summary>
    [Fact(DisplayName = "Rejects report states when the time zone label is missing")]
    public void Rejects_reports_without_time_zone()
    {
        WorkspaceBody body = new();
        InvalidOperationException summary = Assert.Throws<InvalidOperationException>(() => body.Data("summary.month", "{\"accounts\":[],\"summary\":{\"year\":2026,\"month\":4,\"timeZone\":\"\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        InvalidOperationException breakdown = Assert.Throws<InvalidOperationException>(() => body.Data("category.month", "{\"accounts\":[],\"breakdown\":{\"year\":2026,\"month\":4,\"timeZone\":\"\",\"currencies\":[]},\"choices\":{\"accounts\":[],\"categories\":[]},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("requires time zone", summary.Message, StringComparison.Ordinal);
        Assert.Contains("requires time zone", breakdown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that unknown states fail fast.
    /// </summary>
    [Fact(DisplayName = "Rejects unknown workspace screen states")]
    public void Rejects_unknown_state()
    {
        WorkspaceBody body = new();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => body.Data("workspace.unknown", "{\"accounts\":[],\"financial\":{\"name\":\"\",\"currency\":\"\",\"amount\":null},\"status\":{\"error\":\"\",\"notice\":\"\"},\"custom\":false}"));
        Assert.Contains("is not recognized", error.Message, StringComparison.Ordinal);
    }
}
