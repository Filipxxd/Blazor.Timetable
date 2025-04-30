using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Models;
using Timetable.Models.Grid;

namespace Timetable.Services.Display;

internal interface IDisplayService
{
    DisplayType DisplayType { get; }

    Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        PropertyAccessors<TEvent> props) where TEvent : class;
}
