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
				HasColumnHeader = true,
				ColumnCount = 1,
				RowCount = 1,
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
				var headerCell = new Cell<TEvent>
				{
					Id = Guid.NewGuid(),
					DateTime = cellDate,
					Title = $"{cellDate:dddd, dd MMM}"
				};

				var column = new Column<TEvent>
				{
					DayOfWeek = cellDate.DayOfWeek,
					Cells = [headerCell]
				};

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
						Span = (int)Math.Ceiling((config.TimeTo - config.TimeFrom).TotalHours), // order by this when displaying
						IsHeaderEvent = true,
						ColumnIndex = dayIndex,
						RowIndex = 1
					}).ToList();

				headerCell.Events.AddRange(wholeDayEvents);
				grid.Columns.Add(column);
			}

			// Populate each column with hourly cells and events
			foreach (var column in grid.Columns)
			{
				for (int hourIndex = 0; hourIndex < config.Hours.Count(); hourIndex++)
				{
					var hour = config.Hours.ElementAt(hourIndex);
					var cellStartTime = column.HeaderCell.DateTime.Date.AddHours(hour);
					int slotIndex = hour - config.TimeFrom.Hour + 1; // 1-based

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
							Span = (int)Math.Ceiling((props.GetDateTo(e) - props.GetDateFrom(e)).TotalHours),
							IsHeaderEvent = false,
							ColumnIndex = grid.Columns.IndexOf(column) + 1,
							RowIndex = slotIndex
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

			if (orderedDays.Count == 0)
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