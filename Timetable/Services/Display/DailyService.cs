using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class DailyService
{
    public IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        TimetableEventProps<TEvent> props) where TEvent : class
    {
        var rows = new List<GridRow<TEvent>>();

        foreach (var hour in config.Hours)
        {
            var rowStartTime = config.CurrentDate.Date.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
            
            var eventsAtSlot = events.Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                var eventEnd = props.GetDateTo(e);
                
                return eventStart.Date == config.CurrentDate.Date && eventStart.Hour <= hour && eventEnd.Hour > hour;
            });

            var items = eventsAtSlot
                .Select(e => new GridEvent<TEvent>(e, props, config))
                .ToList();

            var gridCell = new GridCell<TEvent>
            {
                Id = Guid.NewGuid(),
                CellTime = rowStartTime,
                Events = items
            };

            gridRow.Cells.Add(gridCell);
            rows.Add(gridRow);
        }

        return rows;
    }
}
