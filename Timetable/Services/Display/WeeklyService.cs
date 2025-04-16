using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display
{
    internal sealed class WeeklyService
    {
        public Grid<TEvent> CreateGrid<TEvent>(
            IList<TEvent> events,
            TimetableConfig config,
            DateTime currentDate,
            CompiledProps<TEvent> props) where TEvent : class
        {
            var cellDates = CalculateGridDates(currentDate, config.Days).ToList();
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
                Title = $"{startDate:dddd d. MMMM} - {endDate:dddd d. MMMM}".CapitalizeWords(),
            };

            // Populate RowPrepend with time slot labels
            foreach (var hour in config.Hours)
            {
                var formattedTime = config.Is24HourFormat
                    ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                    : DateTime.Today.AddHours(hour).ToString("h tt");
                grid.RowPrepend.Add(formattedTime);
            }

            // Assign each day a column index (1-based for CSS Grid)
            int dayIndex = 1;
            foreach (var cellDate in cellDates)
            {
                var column = new Column<TEvent>
                {
                    DayOfWeek = cellDate.DayOfWeek,
                    HeaderCell = new Cell<TEvent>
                    {
                        Id = Guid.NewGuid(),
                        DateTime = cellDate,
                        Title = $"{cellDate:dddd, dd MMM}"
                    }
                };

                // Identify whole-day events
                var wholeDayEvents = weeklyEvents
                    .Where(e =>
                    {
                        var dateFrom = props.GetDateFrom(e);
                        var dateTo = props.GetDateTo(e);
                        return dateFrom.Date == cellDate &&
                           (dateFrom.Hour < config.TimeFrom.Hour || dateTo.Hour > config.TimeTo.Hour);
                    })
                    .Select(e => new EventWrapper<TEvent>
                    {
                        Props = props,
                        Event = e,
                        Id = Guid.NewGuid(),
                        Index = 0, // TODO: Calculate overlapping index if needed
                        Span = (int)Math.Ceiling((config.TimeTo - config.TimeFrom).TotalHours),
                        IsWholeDay = true,
                        DayColumn = dayIndex,
                        StartSlot = 1
                    }).ToList();

                column.HeaderCell.Events.AddRange(wholeDayEvents);
                grid.Columns.Add(column);
            }

            // Populate each column with hourly cells and events
            foreach (var column in grid.Columns)
            {
                for (int hourIndex = 0; hourIndex < config.Hours.Count(); hourIndex++)
                {
                    var hour = config.Hours.ElementAt(hourIndex);
                    var cellStartTime = column.HeaderCell.DateTime.Date.AddHours(hour);
                    int slotIndex = hour - (int)config.TimeFrom.Hour + 1; // 1-based

                    // Find events that start at this hour and fit within the time constraints
                    var cellEvents = weeklyEvents
                        .Where(e =>
                        {
                            var dateFrom = props.GetDateFrom(e);
                            var dateTo = props.GetDateTo(e);
                            return dateFrom.Hour == hour && dateFrom.Hour >= config.TimeFrom.Hour && dateTo.Hour <= config.TimeTo.Hour && dateFrom.Date == cellStartTime.Date && dateTo.Date == cellStartTime.Date;

                        })
                        .Select(e => new EventWrapper<TEvent>
                        {
                            Props = props,
                            Event = e,
                            Id = Guid.NewGuid(),
                            Index = 0, // TODO: Calculate overlapping index if needed
                            Span = (int)Math.Ceiling((props.GetDateTo(e) - props.GetDateFrom(e)).TotalHours),
                            IsWholeDay = false,
                            DayColumn = grid.Columns.IndexOf(column) + 1,
                            StartSlot = slotIndex
                        })
                        .ToList();

                    var cell = new Cell<TEvent>
                    {
                        Id = Guid.NewGuid(),
                        DateTime = cellStartTime,
                        Title = cellStartTime.ToString(config.Is24HourFormat ? @"hh\:mm" : "h tt"),
                        Events = cellEvents
                    };
                    column.Cells.Add(cell);
                }
            }

            return grid;
        }

        private static IEnumerable<DateTime> CalculateGridDates(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
        {
            var dates = new List<DateTime>();
            var orderedDays = configuredDays.OrderBy(d => d).ToList();

            if (!orderedDays.Any())
                return dates;

            var firstDayOfWeek = orderedDays.First();
            var startDate = DateHelper.GetStartOfWeekDate(currentDate, firstDayOfWeek);
            dates.Add(startDate);
            var previousDayValue = (int)firstDayOfWeek;
            var previousDate = startDate;

            foreach (var day in orderedDays.Skip(1))
            {
                var diff = (int)day - previousDayValue;
                if (diff <= 0) diff += 7;
                var nextDate = previousDate.AddDays(diff);
                dates.Add(nextDate);
                previousDayValue = (int)day;
                previousDate = nextDate;
            }

            return dates.Distinct();
        }
    }
}