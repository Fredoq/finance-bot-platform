namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal static class WorkspaceZone
{
    internal const string Default = "Etc/UTC";
    internal static string Id(string value) => Resolve(value).Id;
    internal static bool Try(string value, out string zoneId)
    {
        zoneId = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        TimeZoneInfo? item = Find(value.Trim());
        if (item is null)
        {
            return false;
        }
        zoneId = item.Id;
        return true;
    }
    internal static MonthNote Month(DateTimeOffset when, string value)
    {
        TimeZoneInfo item = Resolve(value);
        DateTimeOffset data = TimeZoneInfo.ConvertTime(when, item);
        return new MonthNote(data.Year, data.Month, item.Id);
    }
    internal static MonthRange Range(int year, int month, string value)
    {
        TimeZoneInfo item = Resolve(value);
        DateTime start = new(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime end = start.AddMonths(1);
        return new MonthRange(Boundary(start, item), Boundary(end, item), item.Id);
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
    private static DateTimeOffset Boundary(DateTime value, TimeZoneInfo zone)
    {
        DateTime item = value;
        while (zone.IsInvalidTime(item))
        {
            item = item.AddMinutes(1);
        }
        if (zone.IsAmbiguousTime(item))
        {
            TimeSpan offset = zone.GetAmbiguousTimeOffsets(item).Max();
            return new DateTimeOffset(item, offset).ToUniversalTime();
        }
        return TimeZoneInfo.ConvertTimeToUtc(item, zone);
    }
    private static TimeZoneInfo Resolve(string value)
    {
        string item = string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
        return Find(item) ?? Find(Default) ?? Find("UTC") ?? TimeZoneInfo.Utc;
    }
    private static TimeZoneInfo? Find(string value)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
