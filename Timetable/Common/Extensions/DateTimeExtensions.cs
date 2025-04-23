using Timetable.Common.Enums;

namespace Timetable.Common.Extensions;

internal static class DateTimeExtensions
{
    public static DateOnly GetNextValidDate(this DateOnly date, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
    {
        return GetValidDate(date, DisplayType.Day, false, days, months);
    }

    public static DateOnly GetValidDate(this DateOnly date, DisplayType displayType, bool reverse, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
    {
        int increment = reverse ? -1 : 1;
        int dayIncrement = (displayType == DisplayType.Week) ? 7 : 1;

        if (displayType == DisplayType.Month)
        {
            do
            {
                date = date.AddMonths(increment);
            } while (!months.Contains((Month)date.Month));
        }

        do
        {
            date = date.AddDays(dayIncrement * increment);
        } while (!date.IsValidDate(days, months));

        return date;
    }

    public static bool IsValidDate(this DateOnly date, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
        => days.Contains(date.DayOfWeek) && months.Contains((Month)date.Month);

    public static DateTime ToDateTimeMidnight(this DateOnly dateOnly)
        => new(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);

    public static DateOnly ToDateOnly(this DateTime dateTime)
        => new(dateTime.Year, dateTime.Month, dateTime.Day);
}
