using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Props;

public sealed class CreateProps<TEvent> where TEvent : class
{
    public RepeatOption Repetition { get; set; } = RepeatOption.Once;
    public DateOnly? RepeatUntil { get; set; }
    public int? RepeatDays { get; set; } = 1;
    public EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;
}
