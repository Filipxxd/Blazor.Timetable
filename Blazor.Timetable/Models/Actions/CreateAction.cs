using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models.Actions;

public sealed class CreateAction<TEvent> where TEvent : class
{
    public Repeatability Repetition { get; set; } = Repeatability.Once;
    public DateOnly? RepeatUntil { get; set; }
    public int? RepeatDays { get; set; } = 1;
    public EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;
}
