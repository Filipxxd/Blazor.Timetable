namespace School_Timetable.Utilities;

internal static class DateHelper
{
    public static DateTime GetStartOfWeekDate(DateTime dt, DayOfWeek firstDayOfWeek) => dt.AddDays(-(int)dt.DayOfWeek + (int)firstDayOfWeek); 
}