namespace Timetable.Structure;

internal sealed class Grid<TEvent> where
    TEvent : class
{
    public required string Title { get; init; }
    public IList<Column<TEvent>> Columns { get; init; } = [];
    public IList<string> RowPrepend { get; init; } = [];

    public required bool HasColumnHeader { get; init; }
    public required bool HasRowHeader { get; init; }
    public required int ColumnCount { get; init; } // pondeli utery atd. (denni ma pouze 1)
    public required int RowCount { get; init; }
}
