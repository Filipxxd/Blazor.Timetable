using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class MonthlyService
{
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
                    Id = Guid.NewGuid(),
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
                        .ToList();

                    cell.Events = cellEvents;
                }

                column.Cells.Add(cell);
            }

            columns.Add(column);
        }

        return new Grid<TEvent>
        {
            Title = $"{currentDate:MMMM YYYY}".CapitalizeWords(),
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

        // Determine the month boundaries.
        var firstOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        // Determine grid start: we want the grid's first cell to be the latest date whose DayOfWeek equals configuredDays[0] and is <= firstOfMonth.
        DayOfWeek gridStartDay = orderedDays.First();
        int diff = (7 + ((int)firstOfMonth.DayOfWeek - (int)gridStartDay)) % 7;
        var gridStart = firstOfMonth.AddDays(-diff);

        // Build grid rows.
        var gridRows = new List<List<DateTime>>();
        DateTime rowStart = gridStart;
        while (true)
        {
            var row = CalculateRowDates(rowStart, orderedDays);

            gridRows.Add(row);
            // Check if at least one cell in the last generated row is on or after lastOfMonth.
            if (row.Last() >= lastOfMonth)
                break;


            // Move to the next row.
            rowStart = rowStart.AddDays(7);
        }

        return gridRows;
    }
    private static List<DateTime> CalculateRowDates(DateTime rowStart, IList<DayOfWeek> orderedDays)
    {
        var dates = new List<DateTime>();
        // The first cell.
        DateTime current = rowStart.Date;
        dates.Add(current);

        int previousDayValue = (int)orderedDays[0];
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