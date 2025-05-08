using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Props;

public sealed class DeleteProps<TEvent> where TEvent : class
{
    public ActionScope Scope { get; set; } = ActionScope.Single;
    public EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;
}
