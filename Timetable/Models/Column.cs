namespace Timetable.Models;

internal sealed class Column<TEvent> where
    TEvent : class
{
    public required DayOfWeek DayOfWeek { get; init; }
    public required int Index { get; init; }
    public IList<Cell<TEvent>> Cells { get; init; } = [];
}
