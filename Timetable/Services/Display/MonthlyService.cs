using Timetable.Configuration;
using Timetable.Structure;

namespace Timetable.Services.Display;

internal sealed class MonthlyService
{
    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        CompiledProps<TEvent> props) where TEvent : class
    {
        throw new NotImplementedException();
    }
}
