namespace Timetable.Structure;

internal sealed class Column<TEvent> where
	TEvent : class
{
	public required DayOfWeek DayOfWeek { get; init; }
	public List<Cell<TEvent>> Cells { get; init; } = [];
}
