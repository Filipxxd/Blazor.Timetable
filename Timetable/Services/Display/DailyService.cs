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
        var headerEvents = events.Where(e => IsHeaderEvent(e, currentDate, config, props))
                                             .Select(e => new EventWrapper<TEvent>
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

            var cellEvents = events.Where(e =>
            {
                var dateFrom = props.GetDateFrom(e);
                var dateTo = props.GetDateTo(e);

                var isInTimeRange = dateFrom.Hour >= config.TimeFrom.Hour || (dateTo.Hour <= config.TimeTo.Hour && dateTo.Minute <= config.TimeTo.Minute);
                var isSameDay = dateFrom.Day == dateTo.Day;
                var startMatches = dateFrom.Hour == cellDateTime.Hour && dateFrom.Minute == cellDateTime.Minute;
                var isThisDay = dateFrom.Day == cellDateTime.Day;
                var isFirstCell = timeSlot.Hour == config.TimeFrom.Hour && timeSlot.Minute == config.TimeFrom.Minute && timeSlot > new TimeOnly(dateFrom.Hour, dateFrom.Minute);

                return isInTimeRange && isSameDay && (startMatches || isFirstCell) && isThisDay;
            })
            .Select(e => CreateNormalCellWrapper(e, config, props))
            .OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

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
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")).ToList();

        return new Grid<TEvent>
        {
            Title = $"{currentDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = [column]
        };
    }

    private static bool IsHeaderEvent<TEvent>(
        TEvent e,
        DateOnly cellDate,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);

        var isOutsideDisplayedTimeRange =
            (eventStart.Hour <= config.TimeFrom.Hour && eventEnd.Hour <= config.TimeFrom.Hour) ||
            (eventStart.Hour >= config.TimeTo.Hour && eventEnd.Hour >= config.TimeTo.Hour);

        var spansMultipleDays = eventStart.Day != eventEnd.Day;
        var notStartThisDate = eventStart.Date.ToDateOnly() < cellDate;

        var isFirstCellDateOfNewGrid =
            spansMultipleDays && notStartThisDate &&
            eventEnd.Date.ToDateOnly() <= cellDate;

        var isFirstCellInNormalGrid = eventStart.Date.ToDateOnly() == cellDate;

        var isPastEndTime = (eventEnd.Hour == 0 && eventEnd.Minute == 0) || eventEnd.ToDateOnly() <= cellDate;

        return (isFirstCellDateOfNewGrid && !isPastEndTime || isFirstCellInNormalGrid) &&
               (isOutsideDisplayedTimeRange || spansMultipleDays);
    }

    private static EventWrapper<TEvent> CreateNormalCellWrapper<TEvent>(
        TEvent e,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var dateFrom = props.GetDateFrom(e);
        var dateTo = props.GetDateTo(e);

        var timeFrom = new TimeOnly(dateFrom.Hour, (dateFrom.Minute / TimetableConstants.TimeSlotInterval) * TimetableConstants.TimeSlotInterval);
        var timeTo = new TimeOnly(dateTo.Hour, (dateTo.Minute / TimetableConstants.TimeSlotInterval) * TimetableConstants.TimeSlotInterval);
        var span = 0;

        while (timeFrom < config.TimeTo && timeFrom < timeTo)
        {
            timeFrom = timeFrom.AddMinutes(TimetableConstants.TimeSlotInterval);
            span++;
        }

        return new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Span = span
        };
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
