using Blazor.Timetable.Models;

namespace Blazor.Timetable.Common.Helpers;

internal static class DisplayServiceHelper
{
    public static IEnumerable<string> GetRowTitles(TimeOnly from, TimeOnly to, bool is24Format = true)
    {
        var hourTo = to >= TimetableConstants.EndOfDay ? 24 : to.Hour;

        return Enumerable
            .Range(from.Hour, hourTo - from.Hour)
            .Select(hour => DateTimeHelper.FormatHour(hour, is24Format));
    }

    public static IList<TimeOnly> GetTimeSlots(TimeOnly timeFrom, TimeOnly timeTo)
    {
        var timeSlots = new List<TimeOnly>();
        var currentTime = timeFrom;

        TimeOnly prevTime;

        while (currentTime < timeTo)
        {
            timeSlots.Add(currentTime);

            prevTime = currentTime;

            currentTime = currentTime.AddMinutes(TimetableConstants.TimeSlotInterval);

            if (prevTime > currentTime || currentTime > timeTo)
                break;
        }

        return timeSlots;
    }

    public static int GetEventSpan<TEvent>(TEvent timetableEvent, TimeOnly timeTo, PropertyAccessors<TEvent> props)
        where TEvent : class
    {
        var eventStart = props.GetDateFrom(timetableEvent);
        var eventEnd = props.GetDateTo(timetableEvent);
        var slotTime = new TimeOnly(eventStart.Hour, eventStart.Minute);
        var endTime = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
        var span = 0;

        var isEndOfDay = timeTo >= TimetableConstants.EndOfDay;

        while (slotTime < endTime && (!isEndOfDay || slotTime < TimetableConstants.EndOfDay))
        {
            span++;
            slotTime = slotTime.AddMinutes(TimetableConstants.TimeSlotInterval);

            if (isEndOfDay && slotTime.Hour == 0 && slotTime.Minute == 0)
            {
                break;
            }
        }

        return span;
    }
}