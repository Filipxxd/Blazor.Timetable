using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly date,
        PropertyAccessors<TEvent> props) where TEvent : class;
}
