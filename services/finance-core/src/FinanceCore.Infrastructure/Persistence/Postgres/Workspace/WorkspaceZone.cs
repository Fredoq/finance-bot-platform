namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal static class WorkspaceZone
{
    internal const string Default = "Etc/UTC";
    internal static string Id(string value)
    {
        string item = string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(item);
            return item;
        }
        catch (TimeZoneNotFoundException)
        {
            return Default;
        }
        catch (InvalidTimeZoneException)
        {
            return Default;
        }
    }
    internal static MonthNote Month(DateTimeOffset when, string value)
    {
        TimeZoneInfo item = Find(value);
        DateTimeOffset data = TimeZoneInfo.ConvertTime(when, item);
        return new MonthNote(data.Year, data.Month, item.Id);
    }
    internal static MonthRange Range(int year, int month, string value)
    {
        string id = Id(value);
        TimeZoneInfo item = Find(id);
        DateTime start = new(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime end = start.AddMonths(1);
        return new MonthRange(new DateTimeOffset(start, item.GetUtcOffset(start)).ToUniversalTime(), new DateTimeOffset(end, item.GetUtcOffset(end)).ToUniversalTime(), id);
    }
    internal sealed record MonthNote
    {
        internal MonthNote(int year, int month, string zoneId)
        {
            Year = year;
            PeriodMonth = month;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
        }
        internal int Year { get; }
        internal int PeriodMonth { get; }
        internal string ZoneId { get; }
    }
    internal sealed record MonthRange
    {
        internal MonthRange(DateTimeOffset startUtc, DateTimeOffset endUtc, string zoneId)
        {
            StartUtc = startUtc;
            EndUtc = endUtc;
            ZoneId = zoneId ?? throw new ArgumentNullException(nameof(zoneId));
        }
        internal DateTimeOffset StartUtc { get; }
        internal DateTimeOffset EndUtc { get; }
        internal string ZoneId { get; }
    }
    private static TimeZoneInfo Find(string value) => TimeZoneInfo.FindSystemTimeZoneById(Id(value));
}
