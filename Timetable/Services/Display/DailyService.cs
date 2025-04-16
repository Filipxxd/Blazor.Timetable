using Timetable.Common.Extensions;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class DailyService
{
    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateTime currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var cellDate = CalculateGridDate(currentDate, config.Days)
            ?? throw new InvalidOperationException("No valid date found for creating the grid.");

        var dailyEvents = events
            .Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                return eventStart.Date == cellDate.Date;
            }).ToList();

        var grid = new Grid<TEvent>
        {
            Title = $"{cellDate:dddd d. MMMM}".CapitalizeWords()
        };

        foreach (var hour in config.Hours)
        {
            var formattedTime = config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt");
            grid.RowPrepend.Add(formattedTime);
        }

        var column = new Column<TEvent>
        {
            DayOfWeek = cellDate.DayOfWeek,
            HeaderCell = new Cell<TEvent>
            {
                Id = Guid.NewGuid(),
                DateTime = cellDate,
                Events = [.. dailyEvents
                    .Where(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);
                        return (dateFrom.Hour < config.TimeFrom.Hour || dateTo.Hour > config.TimeTo.Hour);
                    })
                    .Select(e => new EventWrapper<TEvent>
                    {
                        Props = props,
                        Event = e,
                        Id = Guid.NewGuid(),
                        Index = 0,
                        Span = 1,
                        RowIndex = 1,
                        IsHeaderEvent = true,
                        ColumnIndex = 1
                    })]
            }
        };

        foreach (var hour in config.Hours)
        {
            var cellStartTime = cellDate.AddHours(hour);
            var cellEndTime = cellStartTime.AddHours(1);
            var cellEvents = dailyEvents
                .Where(e =>
                {
                    var dateFrom = props.GetDateFrom(e);
                    var dateTo = props.GetDateTo(e);
                    return dateFrom.Hour == hour && dateFrom.Hour >= config.TimeFrom.Hour && dateTo.Hour <= config.TimeTo.Hour && dateFrom.Date == cellDate.Date && dateTo.Date == cellDate.Date;
                })
                .Select(e => new EventWrapper<TEvent>
                {
                    Props = props,
                    Event = e,
                    Id = Guid.NewGuid(),
                    Index = 0,
                    Span = 1,
                    RowIndex = 1,
                    IsHeaderEvent = true,
                    ColumnIndex = 1
                }).ToList();

            var cell = new Cell<TEvent>
            {
                Id = Guid.NewGuid(),
                DateTime = cellStartTime,
                Events = cellEvents
            };

            column.Cells.Add(cell);
        }

        grid.Columns.Add(column);
        return grid;
    }

    private static DateTime? CalculateGridDate(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        if (configuredDays.Contains(currentDate.DayOfWeek))
            return currentDate;

        var nextDay = configuredDays.FirstOrDefault(day => (int)day > (int)currentDate.DayOfWeek);

        if (nextDay != default)
            return currentDate.Date.AddDays((int)nextDay - (int)currentDate.DayOfWeek);

        return currentDate.Date.AddDays(7 + (int)configuredDays.First() - (int)currentDate.DayOfWeek);
    }
}