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

    public static DateTime GetDateForDay(DateTime startOfWeek, DayOfWeek targetDay)
    {
        var startDayInt = (int)startOfWeek.DayOfWeek;
        var targetDayInt = (int)targetDay;

        return startOfWeek.AddDays((targetDayInt - startDayInt + 7) % 7);
    }

    public static string GetLocalizedName(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysToAdd = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;

        return today.AddDays(daysToAdd).ToString("dddd");
    }

    public static DateTime GetNextAvailableDate(DateTime currentDate, int increment, IEnumerable<DayOfWeek> availableDays)
    {
        if (currentDate.DayOfWeek > availableDays.First())
            return currentDate;

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