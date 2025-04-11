using Timetable.Common.Utilities;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;


internal sealed class WeeklyService
{
    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var cellDates = CalculateGridDates(config.CurrentDate, config.Days);
        var endOfGrid = cellDates.Last().AddDays(1);
        var weeklyEvents = events
            .Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                return eventStart >= cellDates.First() && eventStart < endOfGrid;
            }).ToList();

        var startDate = cellDates.First();
        var endDate = cellDates.Last();

        var grid = new Grid<TEvent>
        {
            Title = $"{startDate:dddd d, MMMM} - {endDate:dddd d, MMMM}"
        };

        foreach (var hour in config.Hours)
        {
            var formattedTime = config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt");

            grid.RowPrepend.Add(formattedTime);
        }

        foreach (var cellDate in cellDates)
        {
            var wholeDayEvents = weeklyEvents
                .Where(e =>
                {
                    var dateFrom = props.GetDateFrom(e);
                    var dateTo = props.GetDateTo(e);
                    return dateFrom.Date == cellDate &&
                           (dateFrom.Hour < config.TimeFrom.Hour || dateTo.Hour > config.TimeTo.Hour);
                }).ToList();

            var newColumn = new Column<TEvent>
            {
                DayOfWeek = cellDate.DayOfWeek,
                HeaderCell = new Cell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    DateTime = cellDate,
                    Events = [.. wholeDayEvents
                        .Select(e => new EventWrapper<TEvent>
                        {
                            Props = props,
                            Event = e,
                            Id = Guid.NewGuid(),
                            Index = 0, // TODO: Calculate the appropriate index
                            Span = 1 // TODO: Calculate the appropriate span
                        })]
                }
            };

            foreach (var hour in config.Hours)
            {
                var cellStartTime = cellDate.AddHours(hour);
                var cellEndTime = cellStartTime.AddHours(1);

                var eventsAtSlot = weeklyEvents
                    .Where(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);
                        return dateFrom < cellEndTime && dateTo > cellStartTime;
                    })
                    .Select(e => new EventWrapper<TEvent>
                    {
                        Props = props,
                        Event = e,
                        Id = Guid.NewGuid(),
                        Index = 0, // TODO: Calculate the appropriate index
                        Span = 1 // TODO: Calculate the appropriate span
                    }).ToList();

                var cell = new Cell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    DateTime = cellStartTime,
                    Events = eventsAtSlot
                };

                newColumn.Cells.Add(cell);
            }

            grid.Columns.Add(newColumn);
        }

        return grid;
    }

    private static IEnumerable<DateTime> CalculateGridDates(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        var dates = new List<DateTime>();
        var startDate = DateHelper.GetStartOfWeekDate(currentDate, configuredDays.First());
        dates.Add(startDate);
        var previousDayValue = (int)configuredDays.First();
        var previousDate = startDate;

        foreach (var day in configuredDays.Skip(1))
        {
            var diff = (int)day - previousDayValue;
            if (diff < 0) diff += 7;

            var nextDate = previousDate.AddDays(diff);
            dates.Add(nextDate);

            previousDayValue = (int)day;
            previousDate = nextDate;
        }

        return dates.Distinct();
    }
}