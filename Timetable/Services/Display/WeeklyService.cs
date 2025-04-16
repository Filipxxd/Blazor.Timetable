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
            var cellDates = CalculateGridDates(currentDate, config.Days);

            var gridEndDate = cellDates.Last();

            var weeklyEvents = events
                .Where(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    return eventStart >= cellDates.First() && eventStart <= gridEndDate;
                }).ToList();

            var startDate = cellDates.First();
            var grid = new Grid<TEvent>
            {
                Title = $"{startDate:dddd d. MMMM} - {gridEndDate:dddd d. MMMM}".CapitalizeWords(),
                HasColumnHeader = true
            };

            // Populate RowPrepend with time slot labels
            foreach (var hour in config.Hours)
            {
                var formattedTime = config.Is24HourFormat
                    ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                    : DateTime.Today.AddHours(hour).ToString("h tt");
                grid.RowHeader.Add(formattedTime);
            }

            // Assign each day a column index (1-based for CSS Grid)
            var dayIndex = 1;
            foreach (var cellDate in cellDates)
            {
                var column = new Column<TEvent>
                {
                    DayOfWeek = cellDate.DayOfWeek,
                    Cells = []
                };

                var headerEvents = weeklyEvents
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
                        Span = props.GetDateTo(e).Day - props.GetDateFrom(e).Day + 1
                    }).ToList();

                var headerCell = new Cell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    DateTime = cellDate,
                    Title = $"{cellDate:dddd, dd MMM}", // for monthly only!
                    IsHeaderCell = true,
                    ColumnIndex = dayIndex,
                    RowIndex = 1, // header
                    Events = headerEvents
                };

                column.Cells.Add(headerCell);
                // TODO: Make celldate dateonly
                for (var hourIndex = 1; hourIndex <= config.Hours.Count(); hourIndex++)
                {
                    var hour = config.Hours.ElementAt(hourIndex - 1);
                    var cellStartTime = cellDate.Date.AddHours(hour);

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
                            Span = (int)Math.Ceiling((props.GetDateTo(e) - props.GetDateFrom(e)).TotalHours),
                        }).ToList();

                    var cell = new Cell<TEvent>
                    {
                        Id = Guid.NewGuid(),
                        DateTime = cellStartTime,
                        Title = cellStartTime.ToString(config.Is24HourFormat ? @"hh\:mm" : "h tt"),
                        IsHeaderCell = false,
                        ColumnIndex = dayIndex,
                        RowIndex = hourIndex + 1,
                        Events = cellEvents
                    };

                    column.Cells.Add(cell);
                }

                grid.Columns.Add(column);
                dayIndex++;
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