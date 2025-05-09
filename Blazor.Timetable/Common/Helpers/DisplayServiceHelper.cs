using Blazor.Timetable.Models;

namespace Blazor.Timetable.Common.Helpers;

internal static class DisplayServiceHelper
{
    public static IEnumerable<string> GetRowTitles(TimeOnly from, TimeOnly to, bool is24Format = true)
        => Enumerable
            .Range(from.Hour, to.Hour - from.Hour)
            .Select(hour => DateTimeHelper.FormatHour(hour, is24Format));

    public static IList<TimeOnly> GetTimeSlots(TimeOnly timeFrom, TimeOnly timeTo)
    {
        var timeSlots = new List<TimeOnly>();
        var currentTime = timeFrom;

        while (currentTime < timeTo)
        {
            timeSlots.Add(currentTime);
            currentTime = currentTime.AddMinutes(TimetableConstants.TimeSlotInterval);

            if (currentTime > timeTo)
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

        while (slotTime < timeTo && slotTime < endTime)
        {
            slotTime = slotTime.AddMinutes(TimetableConstants.TimeSlotInterval);
            span++;
        }

        return span;
    }
}