namespace Blazor.Timetable.Common;

internal static class TimetableConstants
{
    public const int TimeSlotInterval = 15;
    public const int MousedownThreshold = 150;
    public static readonly TimeOnly EndOfDay = new(23, 59);
}
