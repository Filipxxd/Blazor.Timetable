using School_Timetable.Configuration;
using School_Timetable.Structure.Entity;
using School_Timetable.Utilities;

namespace School_Timetable.Services.DisplayTypeServices;

internal sealed class WeeklyService : IDisplayTypeService
{
    public IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        Func<TEvent, DateTime> getDateFrom,
        Func<TEvent, DateTime> getDateTo
    ) where TEvent : class
    {
        var rows = new List<GridRow<TEvent>>();

        var startOfWeek = DateHelper.GetStartOfWeekDate(DateTime.Now, config.SelectedDays.First()); // todo: use CurentDate via config

        foreach (var hour in config.Hours)
        {
            var rowStartTime = startOfWeek.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };

            foreach (var dayOfWeek in config.SelectedDays)
            {
                var dayOffset = (dayOfWeek - startOfWeek.DayOfWeek + 7) % 7;
                var cellDate = startOfWeek.AddDays(dayOffset).Date;
                
                var eventsAtSlot = events.Where(e =>
                {
                    var eventStart = getDateFrom(e);
                    return eventStart.Date == cellDate && eventStart.Hour == hour;
                });

                var items = eventsAtSlot
                    .Select(e =>
                    {
                        var eventStart = getDateFrom(e);
                        var eventEnd = getDateTo(e);

                        if (eventEnd.Hour >= config.TimeTo.Hour && eventStart.Hour <= config.TimeFrom.Hour)
                            return null;

                        var span = (int)(eventEnd - eventStart).TotalHours;
                        return new GridItem<TEvent>
                        {
                            Id = Guid.NewGuid(),
                            Event = e,
                            IsWholeDay = eventEnd.Hour >= config.TimeTo.Hour && eventStart.Hour <= config.TimeFrom.Hour,
                            Span = span
                        };
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