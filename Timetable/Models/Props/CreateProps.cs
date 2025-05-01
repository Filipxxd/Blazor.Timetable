using Timetable.Common.Enums;
using Timetable.Models.Grid;

namespace Timetable.Models.Props;

public sealed class CreateProps<TEvent> where TEvent : class
{
    public RepeatOption Repetition { get; set; } = RepeatOption.Once;
    public DateOnly? RepeatUntil { get; set; }
    public int? RepeatDays { get; set; } = 1;
    public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
}
