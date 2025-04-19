using Timetable.Common.Enums;

namespace Timetable.Common.Helpers;

internal static class DateHelper
{
    public static DateTime GetStartOfWeekDate(DateTime currentDate, DayOfWeek firstDayOfWeek)
    {
        var currentDayOfWeek = (int)currentDate.DayOfWeek;
        var targetDayOfWeek = (int)firstDayOfWeek;

        var diff = (7 + (currentDayOfWeek - targetDayOfWeek)) % 7;

        return currentDate.AddDays(-diff).Date;
    }

    public static DateTime GetDateForDay(DateTime currentDate, DayOfWeek targetDay, DayOfWeek firstDayOfWeek)
    {
        var weekStartDate = GetStartOfWeekDate(currentDate, firstDayOfWeek);

        while (weekStartDate.DayOfWeek != targetDay)
        {
            weekStartDate = weekStartDate.AddDays(1);
        }

        return weekStartDate;
    }

    public static string GetLocalizedName(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysToAdd = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;

        return today.AddDays(daysToAdd).ToString("dddd");
    }

    public static DateTime GetNextAvailableDate(DateTime currentDate, int increment, IEnumerable<DayOfWeek> availableDays)
    {
        var test = currentDate.AddDays(1);

        if (availableDays.Contains(test.DayOfWeek)) return test;

        var newDate = currentDate.AddDays(increment);
        while (!availableDays.Contains(newDate.DayOfWeek))
        {
            newDate = newDate.AddDays(increment > 0 ? 1 : -1);
        }

        return newDate;
    }

    public static int GetIncrement(DisplayType displayType)
    {
        return displayType switch
        {
            DisplayType.Day => 1,
            DisplayType.Week => 7,
            _ => throw new NotImplementedException(),
        };
    }
}