using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;

namespace Blazor.Timetable.Common.Helpers;

internal static class DisplayServiceHelper
{
    public static IEnumerable<string> GetTimeRowTitles(TimeOnly timeFrom, TimeOnly timeTo, bool is24Format = true)
    {
        var hours = Enumerable.Range(timeFrom.Hour, timeTo.Hour - timeFrom.Hour);
        return hours.Select(hour => is24Format
            ? $"{hour}:00"
            : $"{hour % 12} {(hour / 12 < 1 ? "AM" : "PM")}");
    }

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

    public static int GetEventSpan<TEvent>(
        TEvent timetableEvent,
        TimetableConfig config,
        PropertyAccessors<TEvent> props)
        where TEvent : class
    {
        var eventStart = props.GetDateFrom(timetableEvent);
        var eventEnd = props.GetDateTo(timetableEvent);
        var slotTime = new TimeOnly(eventStart.Hour, eventStart.Minute);
        var endTime = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
        var span = 0;

        while (slotTime < config.TimeTo && slotTime < endTime)
        {
            slotTime = slotTime.AddMinutes(TimetableConstants.TimeSlotInterval);
            span++;
        }

        return span;
    }
}