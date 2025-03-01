using School_Timetable.Configuration;
using School_Timetable.Enums;
using School_Timetable.Structure.Entity;

namespace School_Timetable.Services.Display;

internal sealed class DailyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Day;
    
    public IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        Func<TEvent, DateTime> getDateFrom,
        Func<TEvent, DateTime> getDateTo
    ) where TEvent : class
    {
        var rows = new List<GridRow<TEvent>>();

        foreach (var hour in config.Hours)
        {
            var rowStartTime = config.CurrentDate.Date.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
            
            var eventsAtSlot = events.Where(e =>
            {
                var eventStart = getDateFrom(e);
                var eventEnd = getDateTo(e);
                
                return eventStart.Date == config.CurrentDate.Date && eventStart.Hour <= hour && eventEnd.Hour > hour;
            });

            var items = eventsAtSlot
                .Select(e =>
                {
                    var eventStart = getDateFrom(e);
                    var eventEnd = getDateTo(e);
                    
                    return new GridItem<TEvent>
                    {
                        Id = Guid.NewGuid(),
                        Event = e,
                        IsWholeDay = eventEnd.Hour >= config.TimeTo.Hour && eventStart.Hour <= config.TimeFrom.Hour, // todo: add prop for whole day
                        Span = (int)(eventEnd - eventStart).TotalHours
                    };
                })
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
