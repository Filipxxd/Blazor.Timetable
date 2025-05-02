using Timetable.Common.Enums;
using Timetable.Models;
using Timetable.Models.Configuration;
using Timetable.Models.Grid;

namespace Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly date,
        PropertyAccessors<TEvent> props) where TEvent : class;
}
