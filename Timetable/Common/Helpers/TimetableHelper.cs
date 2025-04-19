using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Common.Helpers;

internal static class TimetableHelper
{
    public static List<Cell<TEvent>> CreateCells<TEvent>(
       DateTime cellDate,
       TimetableConfig config,
       IList<TEvent> events,
       CompiledProps<TEvent> props) where TEvent : class
    {
        var cells = new List<Cell<TEvent>>();

        var headerEvents = events
            .Where(e => IsHeaderEvent(e, props, cellDate, config))
            .Select(e => WrapEvent(e, props, isHeader: true))
            .ToList();

        var headerCell = new Cell<TEvent>
        {
            Id = Guid.NewGuid(),
            DateTime = cellDate,
            Title = $"{cellDate:dddd, dd MMM}",
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
                .Where(e => IsRegularEvent(e, props, cellStartTime, config))
                .Select(e => WrapEvent(e, props, isHeader: false))
                .ToList();

            var cell = new Cell<TEvent>
            {
                Id = Guid.NewGuid(),
                DateTime = cellStartTime,
                Title = cellStartTime.ToString(config.Is24HourFormat ? @"hh\:mm" : "h tt"),
                Type = CellType.Normal,
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
        return (dateFrom.Date == cellDate.Date && dateTo.Date != dateFrom.Date) ||
               (dateFrom.Date == cellDate.Date && (dateFrom.Hour < config.TimeFrom.Hour || dateTo.Hour > config.TimeTo.Hour));
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
