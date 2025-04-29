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
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var rows = CalendarHelper.CalculateMonthGridDates(currentDate, config.Days);
        var gridStartDate = rows.First().First();
        var gridEndDate = rows.Last().Last();

        var columnsCount = rows.First().Count;

        var columns = new List<Column<TEvent>>();

        for (var col = 0; col < columnsCount; col++)
        {
            var column = new Column<TEvent>
            {
                DayOfWeek = config.Days.ElementAt(col),
                Index = col + 1,
                Cells = []
            };

            for (var row = 0; row < rows.Count; row++)
            {
                var indexInRow = rows[row].FindIndex(d => d.Month == currentDate.Month);
                var isFirstRowCell = indexInRow == col;

                var cellDate = rows[row][col];

                var cell = new Cell<TEvent>
                {
                    DateTime = cellDate.ToDateTimeMidnight(),
                    Title = $"{cellDate:dd}",
                    Type = CellType.Normal,
                    RowIndex = row + 1,
                    Events = []
                };

                column.Cells.Add(cell);

                if (cellDate.Month != currentDate.Month)
                {
                    cell.Type = CellType.Disabled;
                    continue;
                }

                var maxSpan = columnsCount - (col + 1) + 1;

                if (rows[row].Any(d => d.Month != currentDate.Month && rows[row].First().Month == currentDate.Month)) // eg if is lastrow where possible filling of next month days from right!
                {
                    maxSpan -= rows[row].Count(d => d.Month != currentDate.Month);
                }

                var cellEvents = events
                    .Where(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);

                        var isMultiDay = (dateFrom.ToDateOnly() <= cellDate && dateTo.ToDateOnly() >= cellDate);

                        return dateFrom.ToDateOnly() == cellDate || (isMultiDay && isFirstRowCell);
                    })
                    .Select(e =>
                    {
                        var eventStart = props.GetDateFrom(e);
                        var eventEnd = props.GetDateTo(e);

                        var overlapStart = eventStart.ToDateOnly() >= cellDate ? eventStart.ToDateOnly() : cellDate;
                        var overlapEnd = eventEnd.ToDateOnly() < gridEndDate ? eventEnd.ToDateOnly() : gridEndDate;

                        var overlapDays = (int)Math.Floor((overlapEnd.ToDateTimeMidnight() - overlapStart.ToDateTimeMidnight()).TotalDays + 1);

                        return new EventWrapper<TEvent>
                        {
                            Props = props,
                            Event = e,
                            Span = Math.Min(overlapDays, maxSpan)
                        };
                    }).OrderByDescending(e => e.Span)
                    .ToList();

                cell.Events = cellEvents;
            }

            columns.Add(column);
        }

        return new Grid<TEvent>
        {
            Title = $"{currentDate:MMMM yyyy}".CapitalizeWords(),
            Columns = columns
        };
    }
}
