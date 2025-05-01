using Timetable.Common.Enums;

namespace Timetable.Common.Extensions;

internal static class DateExtensions
{
    public static DateOnly GetValidDateFor(this DateOnly date, DisplayType displayType, ICollection<DayOfWeek> days, ICollection<Month> months, bool inFuture)
    {
        var dayIncrement = inFuture ? 1 : -1;
        var weekIncrement = inFuture ? days.Count : -days.Count;

        switch (displayType)
        {
            case DisplayType.Day:
                date = date.AddDays(dayIncrement);
                break;
            case DisplayType.Week:
                date = date.AddDays(weekIncrement);
                break;
            case DisplayType.Month:
                if (inFuture)
                {
                    var nextMonth = date.Month + 1;
                    var nextYear = date.Year;
                    if (nextMonth > 12)
                    {
                        nextMonth = 1;
                        nextYear++;
                    }
                    date = new DateOnly(nextYear, nextMonth, 1);
                }
                else
                {
                    var previousMonth = date.Month - 1;
                    var previousYear = date.Year;
                    if (previousMonth < 1)
                    {
                        previousMonth = 12;
                        previousYear--;
                    }
                    date = new DateOnly(previousYear, previousMonth, DateTime.DaysInMonth(previousYear, previousMonth));
                }
                break;
        }

        while (!date.IsValidFor(days, months))
        {
            date = date.AddDays(dayIncrement);
        }

        return date;
    }

    public static bool IsValidFor(this DateOnly date, ICollection<DayOfWeek> days, ICollection<Month> months)
    {
        return days.Contains(date.DayOfWeek) && months.Contains((Month)date.Month);
    }

    public static DateTime ToDateTimeMidnight(this DateOnly dateOnly)
        => new(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);

    public static DateOnly ToDateOnly(this DateTime dateTime)
        => new(dateTime.Year, dateTime.Month, dateTime.Day);
}
