using Timetable.Common.Enums;

namespace Timetable.Common.Extensions;

internal static class DateTimeExtensions
{

    public static bool IsValidDate(
    this DateOnly date,
    IEnumerable<DayOfWeek> days,
    IEnumerable<Month> months)
    => days.Contains(date.DayOfWeek)
       && months.Contains((Month)date.Month);

    public static DateOnly EnsureValidDate(
        this DateOnly currentDate,
        DisplayType displayType,
        IEnumerable<DayOfWeek> days,
        IEnumerable<Month> months)
        => currentDate.IsValidDate(days, months)
           ? currentDate
           : currentDate.GetValidDate(displayType, false, days, months);

    public static DateOnly GetValidDate(
           this DateOnly currentDate,
           DisplayType displayType,
           bool reverse,
           IEnumerable<DayOfWeek> days,
           IEnumerable<Month> months)
    {
        var dayList = days.Distinct().OrderBy(d => (int)d).ToList();
        var monthList = months.Distinct().OrderBy(m => (int)m).ToList();

        return displayType switch
        {
            DisplayType.Day => GetByDay(currentDate, reverse, dayList, monthList),
            DisplayType.Week => GetByWeek(currentDate, reverse, dayList, monthList),
            DisplayType.Month => GetByMonth(currentDate, reverse, dayList, monthList),
            _ => throw new ArgumentOutOfRangeException(nameof(displayType))
        };
    }

    static DateOnly GetByDay(
        DateOnly date,
        bool reverse,
        IReadOnlyList<DayOfWeek> days,
        IReadOnlyList<Month> months)
    {
        var step = reverse ? -1 : +1;
        var candidate = date;
        if (reverse
         || !days.Contains(candidate.DayOfWeek)
         || !months.Contains((Month)candidate.Month))
            candidate = candidate.AddDays(step);

        while (!days.Contains(candidate.DayOfWeek)
            || !months.Contains((Month)candidate.Month))
            candidate = candidate.AddDays(step);

        return candidate;
    }

    static DateOnly GetByWeek(
        DateOnly date,
        bool reverse,
        IReadOnlyList<DayOfWeek> days,
        IReadOnlyList<Month> months)
    {
        var first = days[0];
        var offset = ((int)date.DayOfWeek - (int)first + 7) % 7;
        var start = date.AddDays(-offset);
        var candidate = (!reverse && date.DayOfWeek == first && months.Contains((Month)start.Month))
                        ? start
                        : start.AddDays(reverse ? -7 : +7);

        while (!months.Contains((Month)candidate.Month))
            candidate = candidate.AddDays(reverse ? -7 : +7);

        return candidate;
    }

    static DateOnly GetByMonth(
        DateOnly date,
        bool reverse,
        IReadOnlyList<DayOfWeek> days,
        IReadOnlyList<Month> months)
    {
        var current = (Month)date.Month;
        var year = date.Year;
        Month target;

        if (!reverse)
        {
            var after = months.Where(m => (int)m > (int)current);
            if (after.Any()) target = after.Min();
            else { target = months.Min(); year++; }
        }
        else
        {
            var before = months.Where(m => (int)m < (int)current);
            if (before.Any()) target = before.Max();
            else { target = months.Max(); year--; }
        }

        var candidate = new DateOnly(year, (int)target, 1);

        while (!days.Contains(candidate.DayOfWeek))
            candidate = candidate.AddDays(1);

        return candidate;
    }

    public static DateTime ToDateTimeMidnight(this DateOnly dateOnly)
        => new(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);

    public static DateOnly ToDateOnly(this DateTime dateTime)
        => new(dateTime.Year, dateTime.Month, dateTime.Day);
}
