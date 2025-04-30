using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Configuration;
using Timetable.Models;
using Timetable.Models.Grid;

namespace Timetable.Services.Display;

internal sealed class DailyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Day;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        PropertyAccessors<TEvent> props) where TEvent : class
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
        }).Select(e => new CellItem<TEvent>
        {
            EventWrapper = new EventWrapper<TEvent>(e, props),
            Span = 1
        }).OrderByDescending(ci => (ci.EventWrapper.DateTo - ci.EventWrapper.DateFrom).TotalHours).ToList();

        var cells = new List<Cell<TEvent>>
        {
            new() {
                DateTime = currentDate.ToDateTimeMidnight(),
                Type = CellType.Header,
                RowIndex = 1,
                Items = headerEvents
            }
        };

        var timeSlots = GetTimeSlots(config.TimeFrom, config.TimeTo);

        for (var i = 0; i < timeSlots.Count; i++)
        {
            var timeSlot = timeSlots[i];

            var cellDateTime = currentDate.ToDateTimeMidnight().AddHours(timeSlot.Hour).AddMinutes(timeSlot.Minute);

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
                var fitsCellDateTime = timeStart.Hour == timeSlot.Hour && timeStart.Minute == timeSlot.Minute && dateStart.Day == cellDateTime.Day && dateStart.Month == cellDateTime.Month && dateStart.Year == cellDateTime.Year;

                return isInTimeRange && isSameDay && fitsCellDateTime;
            })
            .Select(e => new CellItem<TEvent>()
            {
                EventWrapper = new EventWrapper<TEvent>(e, props),
                Span = GetEventSpan(e, config, props)
            }).OrderByDescending(ci => (ci.EventWrapper.DateTo - ci.EventWrapper.DateFrom).TotalHours).ToList();

            cells.Add(new Cell<TEvent>
            {
                DateTime = cellDateTime,
                Type = CellType.Normal,
                RowIndex = i + (timeSlot.Minute % TimetableConstants.TimeSlotInterval) + 2,
                Items = cellEvents
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
        PropertyAccessors<TEvent> props) where TEvent : class
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

    private static List<TimeOnly> GetTimeSlots(TimeOnly start, TimeOnly end)
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
