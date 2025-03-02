using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Structure.Entity;

namespace Timetable.Services.Display;

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