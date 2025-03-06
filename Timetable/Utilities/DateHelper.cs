namespace Timetable.Utilities;

internal static class DateHelper
{
    public static DateTime GetStartOfWeekDate(DateTime dt, DayOfWeek firstDayOfWeek)
    {
        var currentDayOfWeek = (int)dt.DayOfWeek;
        var targetDayOfWeek = (int)firstDayOfWeek;

        var diff = (7 + (currentDayOfWeek - targetDayOfWeek)) % 7;

        return dt.AddDays(-diff).Date;
    }
}