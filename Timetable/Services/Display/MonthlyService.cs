using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class MonthlyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Month;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var rows = CalculateMonthGridDates(currentDate, config.Days).ToList();
        var gridStartDate = rows.First().First();
        var gridEndDate = rows.Last().Last();

        var columnsCount = rows.First().Count;

        var columns = new List<Column<TEvent>>();

        for (var col = 0; col < rows[0].Count; col++)
        {
            var column = new Column<TEvent>
            {
                DayOfWeek = config.Days.ElementAt(col),
                Index = col + 1,
                Cells = []
            };

            for (var row = 0; row < rows.Count; row++)
            {
                var cellDate = rows[row][col];

                var cell = new Cell<TEvent>
                {
                    DateTime = cellDate.ToDateTimeMidnight(),
                    Title = $"{cellDate:dd}",
                    Type = CellType.Normal,
                    RowIndex = row + 1,
                    Events = []
                };
                if (cellDate.Month != currentDate.Month)
                {
                    cell.Type = CellType.Disabled;
                }
                else
                {
                    var cellEvents = events
                        .Where(e =>
                        {
                            var dateFrom = props.GetDateFrom(e);
                            var dateTo = props.GetDateTo(e);

                            var isFirstGridCell = cellDate == gridStartDate; // TODO per row

                            return dateFrom.ToDateOnly() == cellDate || ((dateFrom.Month == cellDate.Month || dateTo.Month == cellDate.Month) && isFirstGridCell);
                        })
                        .Select(e =>
                        {
                            var eventStart = props.GetDateFrom(e);
                            var eventEnd = props.GetDateTo(e);
                            var overlapStart = eventStart.ToDateOnly() >= cellDate ? eventStart.ToDateOnly() : gridStartDate;
                            var overlapEnd = eventEnd.ToDateOnly() < gridEndDate ? eventEnd.ToDateOnly() : gridEndDate;
                            var overlapDays = (int)Math.Max((overlapEnd.ToDateTimeMidnight() - overlapStart.ToDateTimeMidnight()).TotalDays + 1, 1);
                            var currentDayIndex = config.Days.IndexOf(cellDate.DayOfWeek);
                            var maxSpan = config.Days.Count - currentDayIndex - (overlapStart.Month != overlapEnd.Month ? overlapEnd.Day + 1 : 0);

                            return new EventWrapper<TEvent>
                            {
                                Props = props,
                                Event = e,
                                Span = Math.Min(overlapDays, maxSpan + 1)
                            };
                        })
                        .OrderByDescending(e => e.Span)
                        .ToList();

                    cell.Events = cellEvents;
                }

                column.Cells.Add(cell);
            }

            columns.Add(column);
        }

        return new Grid<TEvent>
        {
            Title = $"{currentDate:MMMM yyyy}".CapitalizeWords(),
            Columns = columns
        };
    }

    private static List<List<DateOnly>> CalculateMonthGridDates(
            DateOnly currentDate,
            IList<DayOfWeek> configuredDays)
    {
        var firstOfMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        var gridStartDay = configuredDays.First();
        var diff = (7 + ((int)firstOfMonth.DayOfWeek - (int)gridStartDay)) % 7;
        var gridStart = firstOfMonth.AddDays(-diff);

        var gridRows = new List<List<DateOnly>>();

        while (true)
        {
            var row = CalculateRowDates(gridStart, configuredDays);

            gridRows.Add(row);
            if (row.Last() >= lastOfMonth)
                break;

            gridStart = gridStart.AddDays(7);
        }

        return gridRows;
    }

    private static List<DateOnly> CalculateRowDates(DateOnly date, IList<DayOfWeek> orderedDays)
    {
        var dates = new List<DateOnly>
        {
            date
        };

        var previousDayValue = (int)orderedDays[0];
        foreach (var day in orderedDays.Skip(1))
        {
            var diff = ((int)day - previousDayValue + 7) % 7;
            diff = diff == 0 ? 7 : diff;
            date = date.AddDays(diff);
            dates.Add(date);
            previousDayValue = (int)day;
        }

        return dates;
    }
}