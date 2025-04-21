using Timetable.Common.Enums;

namespace Timetable.Common.Extensions;

internal static class DateTimeExtensions
{
    public static DateTime GetNextValidDate(this DateTime dateTime, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
    {
        return GetValidDate(dateTime, DisplayType.Day, false, days, months);
    }

    public static DateTime GetValidDate(this DateTime dateTime, DisplayType displayType, bool reverse, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
    {
        int increment = reverse ? -1 : 1;
        int dayIncrement = (displayType == DisplayType.Week) ? 7 : 1;

        if (displayType == DisplayType.Month)
        {
            do
            {
                dateTime = dateTime.AddMonths(increment);
            } while (!months.Contains((Month)dateTime.Month));
        }

        do
        {
            dateTime = dateTime.AddDays(dayIncrement * increment);
        } while (!dateTime.IsValidDateTime(days, months));

        return dateTime;
    }

    public static bool IsValidDateTime(this DateTime dateTime, IEnumerable<DayOfWeek> days, IEnumerable<Month> months)
        => days.Contains(dateTime.DayOfWeek) && months.Contains((Month)dateTime.Month);
}
