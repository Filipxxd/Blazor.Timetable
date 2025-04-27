namespace Timetable.Models;

internal sealed class Grid<TEvent> where
    TEvent : class
{
    public required string Title { get; init; }
    public ICollection<Column<TEvent>> Columns { get; init; } = [];
    public IEnumerable<string> RowTitles { get; init; } = [];

    public Cell<TEvent>? FindCellByEventId(Guid eventId)
    {
        return Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Events.Any(e => e.Id == eventId));
    }

    public Cell<TEvent>? FindCellById(Guid cellId)
    {
        return Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Id == cellId);
    }
}
