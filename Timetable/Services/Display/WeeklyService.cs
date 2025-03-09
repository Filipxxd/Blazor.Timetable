using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Structure;
using Timetable.Utilities;

namespace Timetable.Services.Display;

internal sealed class WeeklyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Week;
    
    public IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        TimetableEventProps<TEvent> props) where TEvent : class
    {
        var rows = new List<GridRow<TEvent>>();
        
        var startOfWeek = DateHelper.GetStartOfWeekDate(config.CurrentDate, config.Days.First());
        
        foreach (var hour in config.Hours)
        {
            var rowStartTime = startOfWeek.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
        
            foreach (var dayOfWeek in config.Days)
            {
                var dayOffset = (dayOfWeek - startOfWeek.DayOfWeek + 7) % 7;
                var cellDate = startOfWeek.AddDays(dayOffset).Date;
                
                var eventsAtSlot = events.Where(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    return eventStart.Date == cellDate && eventStart.Hour == hour;
                });
        
                var items = eventsAtSlot
                    .Select(e =>
                    {
                        var eventStart = props.GetDateFrom(e);
                        var eventEnd = props.GetDateTo(e);
        
                        if (eventEnd.Hour >= config.TimeTo.Hour && eventStart.Hour <= config.TimeFrom.Hour)
                            return null;
                        
                        return new GridEvent<TEvent>(e, props, config);
                    })
                    .Where(item => item != null)
                    .Select(item => item!)
                    .ToList();
        
                var gridCell = new GridCell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    CellTime = cellDate.AddHours(hour),
                    Events = items
                };
        
                gridRow.Cells.Add(gridCell);
            }
            rows.Add(gridRow);
        }
        
        return rows;
    }
}