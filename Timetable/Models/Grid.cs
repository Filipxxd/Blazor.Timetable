namespace Timetable.Models;

internal sealed class Grid<TEvent> where
    TEvent : class
{
    public required string Title { get; init; }
    public ICollection<Column<TEvent>> Columns { get; init; } = [];
    public IEnumerable<string> RowTitles { get; init; } = [];

    public bool HasRowTitles => RowTitles.Count() > 0;
}
