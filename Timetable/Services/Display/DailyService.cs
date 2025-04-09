using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class DailyService
{
    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        //var rows = new List<Row<TEvent>>();

        //foreach (var hour in config.Hours)
        //{
        //	var rowStartTime = config.CurrentDate.Date.AddHours(hour);
        //	var gridRow = new Row<TEvent> { StartTime = rowStartTime };

        //	var eventsAtSlot = events.Where(e =>
        //	{
        //		var eventStart = props.GetDateFrom(e);
        //		var eventEnd = props.GetDateTo(e);

        //		return eventStart.Date == config.CurrentDate.Date && eventStart.Hour <= hour && eventEnd.Hour > hour;
        //	});

        //	var items = eventsAtSlot
        //		.Select(e => new EventWrapper<TEvent>(e, props, config))
        //		.ToList();

        //	var gridCell = new Cell<TEvent>
        //	{
        //		Id = Guid.NewGuid(),
        //		Time = rowStartTime,
        //		Events = items
        //	};

        //	gridRow.Cells.Add(gridCell);
        //	rows.Add(gridRow);
        //}

        return default!;
    }
}
