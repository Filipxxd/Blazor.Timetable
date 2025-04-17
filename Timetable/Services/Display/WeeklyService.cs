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

            var rowTitles = config.Hours.Select(hour =>
                config.Is24HourFormat
                    ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                    : DateTime.Today.AddHours(hour).ToString("h tt")
            ).ToList();

            var dayIndex = 1;
            var columns = new List<Column<TEvent>>();
            foreach (var cellDate in cellDates)
            {
                var column = new Column<TEvent>
                {
                    DayOfWeek = cellDate.DayOfWeek,
                    Index = dayIndex,
                    Cells = TimetableHelper.CreateCells(cellDate, config, weeklyEvents, props)
                };
                columns.Add(column);
                dayIndex++;
            }

            return new Grid<TEvent>
            {
                Title = $"{startDate:dddd d.} - {gridEndDate:dddd d. MMMM yyyy}".CapitalizeWords(),
                RowTitles = rowTitles,
                Columns = columns
            };
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
    }
}