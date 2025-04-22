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
        DateTime currentDate,
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
                    DateTime = cellDate,
                    Type = CellType.Header,
                    RowIndex = 1,
                    Events = headerEvents
                }
            };

            var configHours = config.Hours.ToList();
            for (var hourIndex = 0; hourIndex < configHours.Count; hourIndex++)
            {
                var hour = configHours[hourIndex];
                var cellStartTime = cellDate.Date.AddHours(hour);
                var cellEvents = events.Where(e => IsNormalCellEvent(e, cellDate, hour, config, props))
                                       .Select(e => CreateNormalCellWrapper(e, config, props))
                                       .OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

                cells.Add(new Cell<TEvent>
                {
                    DateTime = cellStartTime,
                    Type = CellType.Normal,
                    RowIndex = hourIndex + 2,
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
        DateTime cellDate,
        DateTime gridStart,
        DateTime gridEndDate,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);

        var isOutsideDisplayedTimeRange =
            (eventStart.Hour <= config.TimeFrom.Hour && eventEnd.Hour <= config.TimeFrom.Hour) ||
            (eventStart.Hour >= config.TimeTo.Hour && eventEnd.Hour >= config.TimeTo.Hour);

        var spansMultipleDays = eventStart.Day != eventEnd.Day;
        var spansMultipleGrids = eventStart.Date < gridStart.Date;

        var isFirstCellDateOfNewGrid =
            spansMultipleDays && spansMultipleGrids &&
            cellDate.Date == gridStart.Date &&
            eventStart.Date < gridStart.Date &&
            eventEnd.Date <= gridEndDate.Date;

        var isFirstCellInNormalGrid = eventStart.Date == cellDate.Date;

        var isPastEndTime = (eventEnd.Hour == 0 && eventEnd.Minute == 0) || eventEnd <= gridStart; // TODO: Check seconds == 0?

        return (isFirstCellDateOfNewGrid && !isPastEndTime || isFirstCellInNormalGrid) &&
               (isOutsideDisplayedTimeRange || spansMultipleDays);
    }

    private static EventWrapper<TEvent> CreateHeaderWrapper<TEvent>(
        TEvent e,
        DateTime gridStart,
        DateTime gridEndDate,
        DateTime cellDate,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var eventStart = props.GetDateFrom(e);
        var eventEnd = props.GetDateTo(e);
        var overlapStart = eventStart > gridStart ? eventStart : gridStart;
        var overlapEnd = eventEnd.Date < gridEndDate.Date.AddDays(1) ? eventEnd : gridEndDate.Date.AddDays(1);
        var overlapDays = Math.Max((overlapEnd - overlapStart).Days + 1, 1);
        var currentDayIndex = config.Days.IndexOf(cellDate.DayOfWeek);
        var maxSpan = config.Days.Count - currentDayIndex;

        return new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Span = Math.Min(overlapDays, maxSpan)
        };
    }

    private static bool IsNormalCellEvent<TEvent>(
        TEvent e,
        DateTime cellDate,
        int hour,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var dateFrom = props.GetDateFrom(e);
        var dateTo = props.GetDateTo(e);
        var isInTimeRange = dateFrom.Hour >= config.TimeFrom.Hour || dateTo.Hour <= config.TimeTo.Hour;
        var isSameDay = dateFrom.Day == dateTo.Day;
        var startHourMatches = dateFrom.Hour == hour;
        var isThisDay = dateFrom.Day == cellDate.Day;

        return isInTimeRange && isSameDay && startHourMatches && isThisDay;
    }

    private static EventWrapper<TEvent> CreateNormalCellWrapper<TEvent>(
        TEvent e,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var dateFrom = props.GetDateFrom(e);
        var dateTo = props.GetDateTo(e);
        var toHour = dateTo.Hour > config.TimeTo.Hour ? config.TimeTo.Hour : dateTo.Hour;

        return new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Span = toHour - dateFrom.Hour
        };
    }

    private static List<DateTime> CalculateGridDates(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        var orderedDays = configuredDays.OrderBy(d => d);
        var startDate = DateHelper.GetStartOfWeekDate(currentDate, orderedDays.First());
        var dates = new List<DateTime> { startDate };
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