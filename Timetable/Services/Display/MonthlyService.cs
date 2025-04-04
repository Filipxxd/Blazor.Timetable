using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class MonthlyService
{
	public IList<Row<TEvent>> CreateGrid<TEvent>(
		IList<TEvent> events,
		TimetableConfig config,
		EventProps<TEvent> props) where TEvent : class
	{
		// var rows = new List<GridRow<TEvent>>();
		// var startOfMonth = new DateTime(config.CurrentDate.Year, config.CurrentDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
		// var daysInMonth = DateTime.DaysInMonth(startOfMonth.Year, startOfMonth.Month);
		//
		// foreach (var dayOfMonth in Enumerable.Range(1, daysInMonth))
		// {
		//     var currentDay = startOfMonth.AddDays(dayOfMonth - 1);
		//
		//     foreach (var hour in config.Hours)
		//     {
		//         var rowStartTime = currentDay.AddHours(hour);
		//         var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
		//
		//         var eventsAtSlot = events.Where(e =>
		//         {
		//             var eventStart = getDateFrom(e);
		//             var eventEnd = getDateTo(e);
		//             return eventStart.Date == currentDay.Date && eventStart.Hour <= hour && eventEnd.Hour > hour;
		//         });
		//
		//         var items = eventsAtSlot
		//             .Select(e =>
		//             {
		//                 var eventStart = getDateFrom(e);
		//                 var eventEnd = getDateTo(e);
		//
		//                 return new GridItem<TEvent>
		//                 {
		//                     Id = Guid.NewGuid(),
		//                     Event = e,
		//                     IsWholeDay = eventEnd.Hour >= config.TimeTo.Hour && eventStart.Hour <= config.TimeFrom.Hour,
		//                     Span = (int)(eventEnd - eventStart).TotalHours
		//                 };
		//             })
		//             .ToList();
		//
		//         var gridCell = new GridCell<TEvent>
		//         {
		//             Id = Guid.NewGuid(),
		//             CellTime = rowStartTime,
		//             Events = items
		//         };
		//
		//         gridRow.Cells.Add(gridCell);
		//         rows.Add(gridRow);
		//     }
		// }
		//
		// return rows;
		throw new NotImplementedException();
	}
}
