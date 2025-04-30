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
        var rows = CalculateMonthGridDates(currentDate, config.Days);
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

                // eg if is lastrow where possible filling of next month days from right!
                if (rows[row].Any(d => d.Month != currentDate.Month && rows[row].First().Month == currentDate.Month))
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

    public static List<List<DateOnly>> CalculateMonthGridDates(DateOnly date, IList<DayOfWeek> days)
    {
        if (days == null || days.Count == 0)
            throw new ArgumentException("configuredDays must contain at least one element.");

        var firstOfMonth = new DateOnly(date.Year, date.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var startDiff = ((int)firstOfMonth.DayOfWeek - (int)days[0] + 7) % 7;
        var gridStartBase = firstOfMonth.AddDays(-startDiff);
        var offsets = days
                              .Select(d => ((int)d - (int)days[0] + 7) % 7)
                              .ToArray();

        var maxWeekIndex = (lastOfMonth.DayNumber - gridStartBase.DayNumber) / 7;

        var firstWeekIndex = 0;
        while (firstWeekIndex <= maxWeekIndex)
        {
            var ws = gridStartBase.AddDays(firstWeekIndex * 7);
            if (offsets.Any(off => ws.AddDays(off).Month == date.Month))
                break;
            firstWeekIndex++;
        }

        var rows = new List<List<DateOnly>>();
        for (var wi = firstWeekIndex; wi <= maxWeekIndex; wi++)
        {
            var ws = gridStartBase.AddDays(wi * 7);
            var row = offsets.Select(off => ws.AddDays(off)).ToList();
            rows.Add(row);
        }

        return rows;
    }
}
