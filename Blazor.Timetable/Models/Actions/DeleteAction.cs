using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Actions;

public sealed class DeleteAction<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Single;
    public EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;
}
