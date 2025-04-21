using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class MonthlyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Month;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateTime currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var rows = CalculateMonthGridDates(currentDate, config.Days).ToList();

        var columnsCount = rows.First().Count;

        var columns = new List<Column<TEvent>>();

        for (var col = 0; col < rows[0].Count; col++)
        {
            var column = new Column<TEvent>
            {
                DayOfWeek = config.Days.ToList().ElementAt(col),
                Index = col + 1,
                Cells = []
            };

            for (var row = 0; row < rows.Count; row++)
            {
                var cellDate = rows[row][col];

                var cell = new Cell<TEvent>
                {
                    DateTime = cellDate,
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
                        .Where(e => TimetableHelper.IsMonthValidEvent(e, props, cellDate, config))
                        .Select(e => TimetableHelper.WrapEvent(e, props, isHeader: true))
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

    public static List<List<DateTime>> CalculateMonthGridDates(
            DateTime currentDate,
            IEnumerable<DayOfWeek> configuredDays)
    {
        var orderedDays = configuredDays.ToList();
        if (orderedDays.Count == 0)
            throw new InvalidOperationException("Configured days cannot be empty.");

        var firstOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        var gridStartDay = orderedDays.First();
        var diff = (7 + ((int)firstOfMonth.DayOfWeek - (int)gridStartDay)) % 7;
        var gridStart = firstOfMonth.AddDays(-diff);

        var gridRows = new List<List<DateTime>>();

        while (true)
        {
            var row = CalculateRowDates(gridStart, orderedDays);

            gridRows.Add(row);
            if (row.Last() >= lastOfMonth)
                break;

            gridStart = gridStart.AddDays(7);
        }

        return gridRows;
    }
    private static List<DateTime> CalculateRowDates(DateTime rowStart, IList<DayOfWeek> orderedDays)
    {
        var dates = new List<DateTime>();
        var current = rowStart.Date;

        dates.Add(current);

        var previousDayValue = (int)orderedDays[0];
        foreach (var day in orderedDays.Skip(1))
        {
            int diff = ((int)day - previousDayValue + 7) % 7;
            diff = diff == 0 ? 7 : diff;
            current = current.AddDays(diff);
            dates.Add(current);
            previousDayValue = (int)day;
        }

        return dates;
    }
}