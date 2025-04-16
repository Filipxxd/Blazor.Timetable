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
        var cellDate = currentDate.Date;

        var todayEvents = events.Where(e =>
        {
            var eventStart = props.GetDateFrom(e);
            return eventStart >= cellDate && eventStart < cellDate.AddDays(1);
        }).ToList();

        var rowHeader = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")
        ).ToList();

        var dayIndex = 1;
        var columns = new List<Column<TEvent>>();

        var column = new Column<TEvent>
        {
            DayOfWeek = cellDate.DayOfWeek,
            Index = dayIndex,
            Cells = CreateCells(cellDate, config, todayEvents, props)
        };
        columns.Add(column);
        dayIndex++;

        return new Grid<TEvent>
        {
            Title = $"{cellDate:dddd d. MMMM}".CapitalizeWords(),
            HasColumnHeader = true,
            RowHeader = rowHeader,
            Columns = columns
        };
    }

    private static List<Cell<TEvent>> CreateCells<TEvent>(
        DateTime cellDate,
        TimetableConfig config,
        IList<TEvent> weeklyEvents,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var cells = new List<Cell<TEvent>>();

        var headerEvents = weeklyEvents
            .Where(e => IsHeaderEvent(e, props, cellDate, config))
            .Select(e => WrapEvent(e, props, isHeader: true))
            .ToList();

        var headerCell = new Cell<TEvent>
        {
            Id = Guid.NewGuid(),
            DateTime = cellDate,
            Title = $"{cellDate:dddd, dd MMM}",
            IsHeaderCell = true,
            RowIndex = 1,
            Events = headerEvents
        };
        cells.Add(headerCell);

        foreach (var hour in config.Hours)
        {
            var hourIndex = config.Hours.ToList().IndexOf(hour);
            var cellStartTime = cellDate.Date.AddHours(hour);
            var cellEvents = weeklyEvents
                .Where(e => IsRegularEvent(e, props, cellStartTime, config))
                .Select(e => WrapEvent(e, props, isHeader: false))
                .ToList();

            var cell = new Cell<TEvent>
            {
                Id = Guid.NewGuid(),
                DateTime = cellStartTime,
                Title = cellStartTime.ToString(config.Is24HourFormat ? @"hh\:mm" : "h tt"),
                IsHeaderCell = false,
                RowIndex = hourIndex + 2,
                Events = cellEvents
            };
            cells.Add(cell);
        }

        return cells;
    }

    private static bool IsHeaderEvent<TEvent>(
        TEvent e,
        CompiledProps<TEvent> props,
        DateTime cellDate,
        TimetableConfig config) where TEvent : class
    {
        var dateFrom = props.GetDateFrom(e);
        var dateTo = props.GetDateTo(e);
        return dateFrom.Date == cellDate.Date &&
               (dateFrom.Hour < config.TimeFrom.Hour || dateTo.Hour > config.TimeTo.Hour);
    }

    private static bool IsRegularEvent<TEvent>(
        TEvent e,
        CompiledProps<TEvent> props,
        DateTime cellStartTime,
        TimetableConfig config) where TEvent : class
    {
        var dateFrom = props.GetDateFrom(e);
        var dateTo = props.GetDateTo(e);
        return dateFrom.Hour == cellStartTime.Hour &&
               dateFrom.Hour >= config.TimeFrom.Hour &&
               dateTo.Hour <= config.TimeTo.Hour &&
               dateFrom.Date == cellStartTime.Date &&
               dateTo.Date == cellStartTime.Date;
    }

    private static EventWrapper<TEvent> WrapEvent<TEvent>(
        TEvent e,
        CompiledProps<TEvent> props,
        bool isHeader) where TEvent : class
    {
        return new EventWrapper<TEvent>
        {
            Props = props,
            Event = e,
            Id = Guid.NewGuid(),
            Span = isHeader
                ? props.GetDateTo(e).Day - props.GetDateFrom(e).Day + 1
                : (int)Math.Ceiling((props.GetDateTo(e) - props.GetDateFrom(e)).TotalHours)
        };
    }
}