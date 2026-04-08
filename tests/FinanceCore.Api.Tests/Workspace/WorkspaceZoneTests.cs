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
        Assert.True(string.Equals(id, "Etc/UTC", StringComparison.Ordinal) || string.Equals(id, "UTC", StringComparison.Ordinal));
        Assert.Equal(zone.Id, id);
        Assert.Equal(TimeSpan.Zero, zone.BaseUtcOffset);
        Assert.False(zone.SupportsDaylightSavingTime);
    }

    /// <summary>
    /// Verifies that ambiguous local month starts use the earliest UTC boundary.
    /// </summary>
    [Fact(DisplayName = "Uses the earliest UTC instant for an ambiguous month start")]
    public void Uses_ambiguous_boundary()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Havana");
        DateTime local = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Unspecified);
        TimeSpan offset = zone.GetAmbiguousTimeOffsets(local).Max();
        DateTimeOffset expected = new(local, offset);
        WorkspaceZone.MonthRange range = WorkspaceZone.Range(2026, 11, "America/Havana");
        Assert.Equal(expected.ToUniversalTime(), range.StartUtc);
        Assert.Equal("America/Havana", range.ZoneId);
    }
}
