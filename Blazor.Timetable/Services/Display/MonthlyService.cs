using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Services.Display;

internal sealed class MonthlyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Month;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly date,
        PropertyAccessors<TEvent> props) where TEvent : class
    {
        var weeks = CalculateMonthGridDates(date, config.Days);
        var monthStart = weeks.First().First();
        var monthEnd = weeks.Last().Last();
        var columnCount = weeks.First().Count;
        var columns = new List<Column<TEvent>>();

        for (var colIndex = 0; colIndex < columnCount; colIndex++)
        {
            var column = new Column<TEvent>
            {
                DayOfWeek = config.Days[colIndex],
                Index = colIndex + 1,
                Cells = []
            };

            for (var rowIndex = 0; rowIndex < weeks.Count; rowIndex++)
            {
                var cellDate = weeks[rowIndex][colIndex];
                var isCurrentMonth = cellDate.Month == date.Month;
                var cell = new Cell<TEvent>
                {
                    DateTime = cellDate.ToDateTimeMidnight(),
                    Title = $"{cellDate:dd}",
                    Type = isCurrentMonth ? CellType.Normal : CellType.Disabled,
                    RowIndex = rowIndex + 1,
                    Items = []
                };

                if (isCurrentMonth)
                {
                    var maxSpan = columnCount - colIndex;
                    var firstOfRowIsCurrent = weeks[rowIndex].First().Month == date.Month;
                    if (weeks[rowIndex].Any(d => d.Month != date.Month) && firstOfRowIsCurrent)
                    {
                        maxSpan -= weeks[rowIndex].Count(d => d.Month != date.Month);
                    }

                    var cellItems = events
                        .Where(timetableEvent =>
                        {
                            var dateStart = props.GetDateFrom(timetableEvent).ToDateOnly();
                            var dateEnd = props.GetDateTo(timetableEvent).ToDateOnly();

                            var spansDay = dateStart <= cellDate && dateEnd >= cellDate;
                            var isFirstInRow = weeks[rowIndex].FindIndex(d => d.Month == date.Month) == colIndex;

                            return dateStart == cellDate || (spansDay && isFirstInRow);
                        })
                        .Select(timetableEvent =>
                        {
                            var dateStart = props.GetDateFrom(timetableEvent).ToDateOnly();
                            var dateEnd = props.GetDateTo(timetableEvent).ToDateOnly();

                            var overlapStart = dateStart >= cellDate
                                ? dateStart
                                : cellDate;
                            var overlapEnd = dateEnd < monthEnd
                                ? dateEnd
                                : monthEnd;
                            var dayCount = (int)
                                ((overlapEnd.ToDateTimeMidnight() - overlapStart.ToDateTimeMidnight()).TotalDays + 1);

                            return new CellItem<TEvent>
                            {
                                EventWrapper = new EventWrapper<TEvent>(timetableEvent, props),
                                Span = Math.Min(dayCount, maxSpan)
                            };
                        })
                        .OrderByDescending(ci => ci.Span)
                        .ToList();

                    cell.Items = cellItems;
                }

                column.Cells.Add(cell);
            }

            columns.Add(column);
        }

        return new Grid<TEvent>
        {
            Title = $"{date:MMMM yyyy}".CapitalizeWords(),
            Columns = columns
        };
    }

    public static List<List<DateOnly>> CalculateMonthGridDates(DateOnly date, IList<DayOfWeek> days)
    {
        var firstOfMonth = new DateOnly(date.Year, date.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var startOffset = ((int)firstOfMonth.DayOfWeek - (int)days[0] + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-startOffset);
        var dayOffsets = days
            .Select(d => ((int)d - (int)days[0] + 7) % 7)
            .ToArray();

        var maxWeekIndex = (lastOfMonth.DayNumber - gridStart.DayNumber) / 7;
        var firstWeekIndex = 0;

        while (firstWeekIndex <= maxWeekIndex)
        {
            var weekStart = gridStart.AddDays(firstWeekIndex * 7);
            if (dayOffsets.Any(off => weekStart.AddDays(off).Month == date.Month))
                break;
            firstWeekIndex++;
        }

        var weeks = new List<List<DateOnly>>();
        for (var weekIndex = firstWeekIndex; weekIndex <= maxWeekIndex; weekIndex++)
        {
            var weekStart = gridStart.AddDays(weekIndex * 7);
            weeks.Add([.. dayOffsets.Select(weekStart.AddDays)]);
        }

        return weeks;
    }
}