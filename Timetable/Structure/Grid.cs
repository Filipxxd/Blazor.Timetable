namespace Timetable.Structure;

internal sealed class Grid<TEvent> where
    TEvent : class
{
    public required string Title { get; init; }
    public IList<Column<TEvent>> Columns { get; init; } = [];
    public IList<string> RowHeader { get; init; } = [];
    public bool HasColumnHeader { get; init; }
}
