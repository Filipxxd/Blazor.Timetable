using School_Timetable.Configuration;
using School_Timetable.Structure.Entity;

namespace School_Timetable.Services.DisplayTypeServices;

internal interface IDisplayTypeService
{
    IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        Func<TEvent, DateTime> getDateFrom,
        Func<TEvent, DateTime> getDateTo
    ) where TEvent : class;
}