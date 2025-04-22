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

        var weeklyEvents = events.Where(e =>
        {
            var eventStart = props.GetDateFrom(e);
            var eventEnd = props.GetDateTo(e);

            return eventStart >= gridStart || eventEnd < gridEndDate.AddDays(1);
        }).ToList();

        var rowTitles = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")
        ).ToList();

        var dayIndex = 1;
        var columns = new List<Column<TEvent>>();
        foreach (var cellDate in cellDates)
        {
            var cells = new List<Cell<TEvent>>();

            var maxSpan = config.Days.Count() - config.Days.ToList().IndexOf(cellDate.DayOfWeek);

            var headerEvents = events
                .Where(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    var eventEnd = props.GetDateTo(e);

                    var isOutsideDisplayedTimeRange = eventStart.Hour < config.TimeFrom.Hour && eventEnd.Hour > config.TimeTo.Hour;
                    var spansMultipleDays = eventStart.Day != eventEnd.Day;
                    var spansMultipleGrids = eventStart.Date < gridStart.Date;

                    var isFirstCellDateOfNewGrid = spansMultipleDays && spansMultipleGrids && cellDate.Date == gridStart.Date && eventStart.Date < gridStart.Date && eventEnd.Date <= gridEndDate.Date;
                    var isFirstCellInNormalGrid = eventStart.Date == cellDate.Date;

                    var title = props.GetTitle(e);

                    if (title.Contains("istory"))
                    {

                    }

                    if (isFirstCellDateOfNewGrid && ((eventEnd.Hour != 0 && eventEnd.Minute != 0) || eventEnd <= gridStart))
                        return false;

                    return (isFirstCellDateOfNewGrid || isFirstCellInNormalGrid) && (isOutsideDisplayedTimeRange || spansMultipleDays);
                })
                .Select(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    var eventEnd = props.GetDateTo(e);

                    var overlapStart = eventStart > gridStart ? eventStart : gridStart;
                    var overlapEnd = eventEnd.Date < gridEndDate.Date.AddDays(1) ? eventEnd : gridEndDate.Date.AddDays(1);

                    var overlapDays = (overlapEnd - overlapStart).Days + 1;
                    var intendedSpan = overlapDays > 0 ? overlapDays : 1;

                    return new EventWrapper<TEvent>
                    {
                        Props = props,
                        Event = e,
                        Span = Math.Min(intendedSpan, maxSpan)
                    };
                }).ToList();

            var headerCell = new Cell<TEvent>
            {
                DateTime = cellDate,
                Type = CellType.Header,
                RowIndex = 1,
                Events = headerEvents
            };
            cells.Add(headerCell);

            foreach (var hour in config.Hours)
            {
                var hourIndex = config.Hours.ToList().IndexOf(hour);
                var cellStartTime = cellDate.Date.AddHours(hour);
                var cellEvents = events
                    .Where(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);

                        var isInTimeRange = dateFrom.Hour >= config.TimeFrom.Hour || dateTo.Hour <= config.TimeTo.Hour;
                        var isSameDay = dateFrom.Day == dateTo.Day;
                        var startHourFits = dateFrom.Hour == hour;
                        var isThisDay = dateFrom.Day == cellDate.Day;

                        var title = props.GetTitle(e);
                        if (title.Contains("Fuckup") && cellDate.Day == 23)
                        {

                        }

                        return isInTimeRange && isSameDay && startHourFits && isThisDay;
                    })
                    .Select(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);

                        var overlapStart = dateFrom > gridStart ? dateFrom : gridStart;
                        var overlapEnd = dateTo.Date < gridEndDate.Date.AddDays(1) ? dateTo : gridEndDate.Date.AddDays(1);

                        var overlapDays = (overlapEnd - overlapStart).Days + 1;
                        var intendedSpan = overlapDays > 0 ? overlapDays : 1;

                        var toHour = dateTo.Hour > config.TimeTo.Hour ? config.TimeTo.Hour : dateTo.Hour;

                        return new EventWrapper<TEvent>
                        {
                            Props = props,
                            Event = e,
                            Span = toHour - props.GetDateFrom(e).Hour
                        };
                    })
                    .OrderByDescending(e => e.Span)
                    .ToList();

                var cell = new Cell<TEvent>
                {
                    DateTime = cellStartTime,
                    Type = CellType.Normal,
                    RowIndex = hourIndex + 2,
                    Events = cellEvents
                };
                cells.Add(cell);
            }

            var column = new Column<TEvent>
            {
                DayOfWeek = cellDate.DayOfWeek,
                Index = dayIndex,
                Cells = cells
            };

            columns.Add(column);
            dayIndex++;
        }

        return new Grid<TEvent>
        {
            Title = $"{gridStart:dddd d. MMMM} - {gridEndDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = columns
        };
    }

    private static IEnumerable<DateTime> CalculateGridDates(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        var orderedDays = configuredDays.OrderBy(d => d).ToList();
        if (orderedDays.Count == 0)
            throw new InvalidOperationException();

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