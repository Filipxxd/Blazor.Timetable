using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class;
}
