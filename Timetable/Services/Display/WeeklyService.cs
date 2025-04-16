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

            var startDate = cellDates.First();
            var gridEndDate = cellDates.Last();

            var weeklyEvents = events.Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                return eventStart >= startDate && eventStart < gridEndDate.AddDays(1);
            }).ToList();

            var grid = new Grid<TEvent>
            {
                Title = $"{startDate:dddd d. MMMM} - {gridEndDate:dddd d. MMMM}".CapitalizeWords(),
                HasColumnHeader = true,
                RowHeader = [],
                Columns = []
            };

            grid.RowHeader = [.. config.Hours.Select(hour =>
                config.Is24HourFormat
                    ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                    : DateTime.Today.AddHours(hour).ToString("h tt")
            )];

            var dayIndex = 1;
            foreach (var cellDate in cellDates)
            {
                var column = new Column<TEvent>
                {
                    DayOfWeek = cellDate.DayOfWeek,
                    Cells = CreateCells(cellDate, dayIndex, config, weeklyEvents, props)
                };
                grid.Columns.Add(column);
                dayIndex++;
            }

            return grid;
        }

        private static IEnumerable<DateTime> CalculateGridDates(DateTime currentDate, IEnumerable<DayOfWeek> configuredDays)
        {
            var orderedDays = configuredDays.OrderBy(d => d).ToList();
            if (orderedDays.Count == 0)
                throw new InvalidOperationException();

            var startDate = DateHelper.GetStartOfWeekDate(currentDate, orderedDays.First());
            var dates = new List<DateTime> { startDate };
            var previousDate = startDate;
            var previousDayValue = (int)orderedDays.First();

            foreach (var day in orderedDays.Skip(1))
            {
                var diff = ((int)day - previousDayValue + 7) % 7;
                diff = diff == 0 ? 7 : diff;
                var nextDate = previousDate.AddDays(diff);
                dates.Add(nextDate);
                previousDayValue = (int)day;
                previousDate = nextDate;
            }

            return dates;
        }

        private static List<Cell<TEvent>> CreateCells<TEvent>(
            DateTime cellDate,
            int dayIndex,
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
                ColumnIndex = dayIndex,
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
                    ColumnIndex = dayIndex,
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
}