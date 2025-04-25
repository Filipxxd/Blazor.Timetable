using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class DailyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Day;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var headerEvents = events.Where(e =>
        {
            var eventStart = props.GetDateFrom(e);
            var eventEnd = props.GetDateTo(e);

            var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
            var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

            var dateStart = eventStart.ToDateOnly();
            var dateEnd = eventEnd.ToDateOnly();

            var isOutOfTimeRange = timeStart < config.TimeFrom && timeEnd <= config.TimeFrom;
            var isMultiDay = eventStart.Day != eventEnd.Day;

            var startsInPreviousView = isMultiDay && dateStart < currentDate;
            var startsInthisCell = dateStart == currentDate;

            return (startsInPreviousView || startsInthisCell) &&
                   (isOutOfTimeRange || isMultiDay);
        }).Select(e => new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Span = 1
        }).OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

        var cells = new List<Cell<TEvent>>
        {
            new() {
                DateTime = currentDate.ToDateTimeMidnight(),
                Type = CellType.Header,
                RowIndex = 1,
                Events = headerEvents
            }
        };

        var timeSlots = GetTimeSlots(config.TimeFrom, config.TimeTo);

        for (var i = 0; i < timeSlots.Count; i++)
        {
            var timeSlot = timeSlots[i];

            var cellDateTime = currentDate.ToDateTimeMidnight().AddHours(timeSlot.Hour).AddMinutes(timeSlot.Minute);

            // TODO: test all variations of date & time, also then test with header events also
            var cellEvents = events.Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                var eventEnd = props.GetDateTo(e);

                var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

                var dateStart = eventStart.ToDateOnly();
                var dateEnd = eventEnd.ToDateOnly();

                var isInTimeRange = timeStart >= config.TimeFrom && timeEnd <= config.TimeTo;
                var isSameDay = dateStart.Day == dateStart.Day;
                var fitsCellDateTime = timeStart.Hour == cellDateTime.Hour && timeStart.Minute == cellDateTime.Minute && dateStart.Day == cellDateTime.Day;

                return isInTimeRange && isSameDay && fitsCellDateTime;
            })
            .Select(e =>
                   new EventWrapper<TEvent>
                   {
                       Props = props,
                       Event = e,
                       Span = GetEventSpan(e, config, props)
                   }
            ).OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();  // todo: test ordering by duration

            cells.Add(new Cell<TEvent>
            {
                DateTime = cellDateTime,
                Type = CellType.Normal,
                RowIndex = i + (timeSlot.Minute % TimetableConstants.TimeSlotInterval) + 2,
                Events = cellEvents
            });
        }

        var column = new Column<TEvent>
        {
            DayOfWeek = currentDate.DayOfWeek,
            Index = 1,
            Cells = cells
        };

        var rowTitles = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? $"{hour}:00"
                : $"{hour % 12} {(hour / 12 < 1 ? "AM" : "PM")}");

        return new Grid<TEvent>
        {
            Title = $"{currentDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = [column]
        };
    }

    private static int GetEventSpan<TEvent>(
        TEvent e,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);

        var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
        var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

        var span = 0;

        while (timeStart < config.TimeTo && timeStart < timeEnd)
        {
            timeStart = timeStart.AddMinutes(TimetableConstants.TimeSlotInterval);
            span++;
        }

        return span;
    }

    private static List<TimeOnly> GetTimeSlots(TimeOnly start, TimeOnly end) // test
    {
        var timeSlots = new List<TimeOnly>();

        var current = start;
        while (current < end)
        {
            timeSlots.Add(current);
            current = current.AddMinutes(TimetableConstants.TimeSlotInterval);

            if (current > end)
                break;
        }

        return timeSlots;
    }
}
