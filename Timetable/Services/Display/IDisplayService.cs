using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateTime currentDate,
        CompiledProps<TEvent> props) where TEvent : class;
}
