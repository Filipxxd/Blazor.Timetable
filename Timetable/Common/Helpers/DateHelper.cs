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
}