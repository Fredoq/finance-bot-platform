using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers time zone normalization and month boundary behavior.
/// </summary>
public sealed class WorkspaceZoneTests
{
    /// <summary>
    /// Verifies that an invalid time zone falls back to a resolvable UTC zone.
    /// </summary>
    [Fact(DisplayName = "Normalizes an invalid time zone to a resolvable UTC fallback")]
    public void Normalizes_invalid_zone()
    {
        string id = WorkspaceZone.Id("Mars/Olympus");
        var zone = TimeZoneInfo.FindSystemTimeZoneById(id);
        Assert.Equal(zone.Id, id);
    }

    /// <summary>
    /// Verifies that ambiguous local month starts use the earliest UTC boundary.
    /// </summary>
    [Fact(DisplayName = "Uses the earliest UTC instant for an ambiguous month start")]
    public void Uses_ambiguous_boundary()
    {
        WorkspaceZone.MonthRange range = WorkspaceZone.Range(2026, 11, "America/Havana");
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero), range.StartUtc);
        Assert.Equal("America/Havana", range.ZoneId);
    }
}
