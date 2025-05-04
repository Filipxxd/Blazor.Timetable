using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Props;

public sealed class UpdateProps<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Single;
    public EventWrapper<TEvent> Original { get; set; } = default!;
    public EventWrapper<TEvent> New { get; set; } = default!;
}
