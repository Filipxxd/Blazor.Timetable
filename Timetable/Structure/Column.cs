namespace Timetable.Structure;

internal sealed class Column<TEvent> where
    TEvent : class
{
    public required DayOfWeek DayOfWeek { get; init; }
    public required Cell<TEvent> HeaderCell { get; init; }
    public IList<Cell<TEvent>> Cells { get; init; } = [];
}
