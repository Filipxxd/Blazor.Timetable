using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class WeeklyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Week;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var cellDates = CalculateGridDates(currentDate, config.Days);
        var gridStart = cellDates.First();
        var gridEndDate = cellDates.Last();

        var columns = cellDates.Select((cellDate, index) =>
        {
            var headerEvents = events.Where(e => IsHeaderEvent(e, cellDate, gridStart, gridEndDate, config, props))
                                     .Select(e => CreateHeaderWrapper(e, gridStart, gridEndDate, cellDate, config, props))
                                     .OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

            var cells = new List<Cell<TEvent>>
            {
                new() {
                    DateTime = cellDate.ToDateTimeMidnight(),
                    Type = CellType.Header,
                    RowIndex = 1,
                    Events = headerEvents
                }
            };

            var timeSlots = GetTimeSlots(config.TimeFrom, config.TimeTo);

            for (var i = 0; i < timeSlots.Count; i++)
            {
                var timeSlot = timeSlots[i];

                var cellStartTime = cellDate.ToDateTimeMidnight().AddHours(timeSlot.Hour).AddMinutes(timeSlot.Minute);

                var cellEvents = events.Where(e =>
                {
                    var dateFrom = props.GetDateFrom(e);
                    var dateTo = props.GetDateTo(e);

                    var isInTimeRange = dateFrom.Hour >= config.TimeFrom.Hour || (dateTo.Hour <= config.TimeTo.Hour && dateTo.Minute <= config.TimeTo.Minute);
                    var isSameDay = dateFrom.Day == dateTo.Day;
                    var startMatches = dateFrom.Hour == timeSlot.Hour && dateFrom.Minute >= timeSlot.Minute && dateFrom.Minute < timeSlot.Minute + TimetableConstants.TimeSlotInterval;
                    var isThisDay = dateFrom.Day == cellDate.Day;
                    var isFirstCell = timeSlot.Hour == config.TimeFrom.Hour && timeSlot.Minute == config.TimeFrom.Minute && timeSlot > new TimeOnly(dateFrom.Hour, dateFrom.Minute);

                    return isInTimeRange && isSameDay && isThisDay && (startMatches || isFirstCell);
                })
                .Select(e => CreateNormalCellWrapper(e, config, props))
                .OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

                cells.Add(new Cell<TEvent>
                {
                    DateTime = cellStartTime,
                    Type = CellType.Normal,
                    RowIndex = i + (timeSlot.Minute % TimetableConstants.TimeSlotInterval) + 2,
                    Events = cellEvents
                });
            }

            return new Column<TEvent>
            {
                DayOfWeek = cellDate.DayOfWeek,
                Index = index + 1,
                Cells = cells
            };
        }).ToList();

        var rowTitles = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")).ToList();

        return new Grid<TEvent>
        {
            Title = $"{gridStart:dddd d. MMMM} - {gridEndDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = columns
        };
    }

    private static bool IsHeaderEvent<TEvent>(
        TEvent e,
        DateOnly cellDate,
        DateOnly gridStart,
        DateOnly gridEndDate,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);

        var isOutsideDisplayedTimeRange =
            (eventStart.Hour <= config.TimeFrom.Hour && eventEnd.Hour <= config.TimeFrom.Hour) ||
            (eventStart.Hour >= config.TimeTo.Hour && eventEnd.Hour >= config.TimeTo.Hour);

        var spansMultipleDays = eventStart.Day != eventEnd.Day;
        var spansMultipleGrids = eventStart.ToDateOnly() < gridStart;

        var isFirstCellDateOfNewGrid =
            spansMultipleDays && spansMultipleGrids &&
            cellDate == gridStart &&
            eventStart.ToDateOnly() < gridStart &&
            eventEnd.ToDateOnly() <= gridEndDate;

        var isFirstCellInNormalGrid = eventStart.ToDateOnly() == cellDate;

        var isPastEndTime = (eventEnd.Hour == 0 && eventEnd.Minute == 0) || eventEnd.ToDateOnly() <= gridStart;

        return (isFirstCellDateOfNewGrid && !isPastEndTime || isFirstCellInNormalGrid) &&
               (isOutsideDisplayedTimeRange || spansMultipleDays);
    }

    private static EventWrapper<TEvent> CreateHeaderWrapper<TEvent>(
        TEvent e,
        DateOnly gridStart,
        DateOnly gridEndDate,
        DateOnly cellDate,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);
        var overlapStart = eventStart.ToDateOnly() > gridStart ? eventStart.ToDateOnly() : gridStart;
        var overlapEnd = eventEnd.ToDateOnly() < gridEndDate.AddDays(1) ? eventEnd.ToDateOnly() : gridEndDate.AddDays(1);
        var overlapDays = (int)Math.Max((overlapEnd.ToDateTimeMidnight() - overlapStart.ToDateTimeMidnight()).TotalDays + 1, 1);
        var currentDayIndex = config.Days.IndexOf(cellDate.DayOfWeek);
        var maxSpan = config.Days.Count - currentDayIndex;

        return new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Span = Math.Min(overlapDays, maxSpan)
        };
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

    private static List<DateOnly> CalculateGridDates(DateOnly currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        var orderedDays = configuredDays.OrderBy(d => d);
        var startDate = DateHelper.GetStartOfWeekDate(currentDate, orderedDays.First());
        var dates = new List<DateOnly> { startDate };
        var previousDate = startDate;
        var previousDayValue = (int)orderedDays.First();

        foreach (var day in orderedDays.Skip(1))
        {
            var diff = ((int)day - previousDayValue + 7) % 7;
            diff = diff == 0 ? 7 : diff;
            var nextDate = previousDate.AddDays(diff);
            dates.Add(nextDate);
            previousDayValue = (int)day;
            previousDate = nextDate;
        }
        return dates;
    }
}