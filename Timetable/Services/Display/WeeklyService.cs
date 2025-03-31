using System;
using System.Collections.Generic;
using System.Linq;
using Timetable.Common.Utilities;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal class WeeklyService
{
	public IList<GridRow<TEvent>> CreateGrid<TEvent>(
		IList<TEvent> events,
		TimetableConfig config,
		TimetableEventProps<TEvent> props) where TEvent : class
	{
		var rows = new List<GridRow<TEvent>>();
		var startOfWeek = DateHelper.GetStartOfWeekDate(config.CurrentDate, config.Days.First());
		var endOfWeek = startOfWeek.AddDays(7);

		var wholeDayRow = new GridRow<TEvent> { IsWholeDayRow = true };

		foreach (var dayOfWeek in config.Days)
		{
			var dayOffset = (dayOfWeek - startOfWeek.DayOfWeek + 7) % 7;
			var cellDate = startOfWeek.AddDays(dayOffset).Date;

			var wholeDayEvents = events
				.Where(e =>
				{
					var eventStart = props.GetDateFrom(e);
					var eventEnd = props.GetDateTo(e);
					return eventStart.Date == cellDate &&
					   (eventStart.Hour < config.TimeFrom.Hour || eventEnd.Hour > config.TimeTo.Hour);
				})
				.Select(e => new GridEvent<TEvent>(e, props, config))
				.ToList();

			var cell = new GridCell<TEvent>
			{
				Id = Guid.NewGuid(),
				CellTime = cellDate,
				Events = wholeDayEvents
			};

			wholeDayRow.Cells.Add(cell);
		}

		rows.Add(wholeDayRow);

		// Create hourly rows for each day.
		foreach (var hour in config.Hours)
		{
			var gridRow = new GridRow<TEvent>
			{
				RowStartTime = startOfWeek.AddHours(hour),
				IsWholeDayRow = false
			};

			foreach (var dayOfWeek in config.Days)
			{
				var dayOffset = (dayOfWeek - startOfWeek.DayOfWeek + 7) % 7;
				var cellDate = startOfWeek.AddDays(dayOffset).Date;

				var eventsAtSlot = events
					.Where(e =>
					{
						var eventStart = props.GetDateFrom(e);
						return eventStart.Date == cellDate && eventStart.Hour == hour &&
							   eventStart >= startOfWeek && eventStart < endOfWeek;
					})
					.Select(e => new GridEvent<TEvent>(e, props, config))
					.Where(item => !item.IsWholeDay)
					.ToList();

				var cell = new GridCell<TEvent>
				{
					Id = Guid.NewGuid(),
					CellTime = cellDate.AddHours(hour),
					Events = eventsAtSlot
				};

				gridRow.Cells.Add(cell);
			}

			rows.Add(gridRow);
		}

		return rows;
	}
}