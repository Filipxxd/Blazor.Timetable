using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Actions;

public sealed class UpdateAction<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Single;
    public EventDescriptor<TEvent> Original { get; set; } = default!;
    public EventDescriptor<TEvent> New { get; set; } = default!;
}
