using Timetable.Common.Utilities;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class WeeklyService
{
    public TimetableGrid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        EventProps<TEvent> props) where TEvent : class
    {
        var rows = new List<Row<TEvent>>();

        var cellDates = CalculateGridDates(config.CurrentDate, config.Days);

        var endOfGrid = cellDates.Last().AddDays(1);

        var weeklyEvents = events
            .Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                return eventStart >= cellDates.First() && eventStart < endOfGrid;
            })
            .Select(e => new EventWrapper<TEvent>(e, props, config))
            .ToList();

        var timetableGrid = new TimetableGrid<TEvent>();

        var wholeDayRow = new Row<TEvent>();

        foreach (var cellDate in cellDates)
        {
            var wholeDayEvents = weeklyEvents
                .Where(e =>
                {
                    return e.DateFrom.Date == cellDate &&
                           (e.DateFrom.Hour < config.TimeFrom.Hour || e.DateTo.Hour > config.TimeTo.Hour);
                }).ToList();

            var cell = new Cell<TEvent>
            {
                Id = Guid.NewGuid(),
                Time = cellDate,
                Events = wholeDayEvents
                // TODO SPAN VIA DATE THIS x DATE TO
            };
            wholeDayRow.Cells.Add(cell);
        }

        timetableGrid.HeaderRow = wholeDayRow;

        foreach (var hour in config.Hours)
        {
            var gridRow = new Row<TEvent>
            {
                StartTime = cellDates.First().AddHours(hour)
            };

            foreach (var cellDate in cellDates)
            {
                var cellStartTime = cellDate.AddHours(hour);
                var cellEndTime = cellStartTime.AddHours(1);

                var eventsAtSlot = weeklyEvents
                    .Where(e => e.DateFrom < cellEndTime && e.DateTo > cellStartTime)
                    .Where(item => !item.IsHeaderEvent)
                    .ToList();

                var cell = new Cell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    Time = cellStartTime,
                    Events = eventsAtSlot
                };
                gridRow.Cells.Add(cell);
            }
            rows.Add(gridRow);
        }

        timetableGrid.Rows = rows;
        return timetableGrid;
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
            if (diff < 0)
            {
                diff += 7;
            }

            var nextDate = previousDate.AddDays(diff);
            dates.Add(nextDate);

            previousDayValue = (int)day;
            previousDate = nextDate;
        }
        return dates;
    }
}