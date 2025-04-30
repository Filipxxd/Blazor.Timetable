using Timetable.Common.Enums;
using Timetable.Models.Grid;

namespace Timetable.Models.Props;

public sealed class DeleteProps<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Current;
    public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
}
