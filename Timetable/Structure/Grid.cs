namespace Timetable.Structure;

internal sealed class Grid<TEvent> where
	TEvent : class
{
	public required string Title { get; init; }
	public IList<Column<TEvent>> Columns { get; init; } = [];
	public IList<string> RowPrepend { get; init; } = [];

	public IEnumerable<string> RowHeaders { get; init; } = [];
	public bool HasRowHeader => RowHeaders.Any();

	public required bool HasColumnHeader { get; init; } // daily -> none, week & month dayOfWeek
	public required int ColumnCount { get; init; } // pondeli utery atd. (denni ma pouze 1)
	public required int RowCount { get; init; } // -> movable rows (without header row & initial row)
}
