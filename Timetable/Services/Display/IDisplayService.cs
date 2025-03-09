using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    public IList<GridRow<TEvent>> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        TimetableEventProps<TEvent> props) where TEvent : class;
}