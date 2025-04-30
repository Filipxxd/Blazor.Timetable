using Timetable.Common.Enums;
using Timetable.Models.Grid;

namespace Timetable.Models.Props;

public sealed class UpdateProps<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Current;
    public EventWrapper<TEvent> Original { get; set; } = default!;
    public EventWrapper<TEvent> New { get; set; } = default!;
}
