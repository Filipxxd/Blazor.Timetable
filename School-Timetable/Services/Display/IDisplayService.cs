using School_Timetable.Configuration;
using School_Timetable.Enums;
using School_Timetable.Structure.Entity;

namespace School_Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }
    
    IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        Func<TEvent, DateTime> getDateFrom,
        Func<TEvent, DateTime> getDateTo
    ) where TEvent : class;
}