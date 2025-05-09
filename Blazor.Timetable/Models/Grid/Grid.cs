namespace Blazor.Timetable.Models.Grid;

internal sealed class Grid<TEvent> where
    TEvent : class
{
    public required string Title { get; init; }
    public ICollection<Column<TEvent>> Columns { get; init; } = [];
    public IEnumerable<string> RowTitles { get; init; } = [];

    public CellItem<TEvent>? FindItemByItemId(Guid itemId)
    {
        return Columns.SelectMany(col => col.Cells)
                           .SelectMany(cell => cell.Items)
                           .FirstOrDefault(item => item.Id == itemId);
    }

    public Cell<TEvent>? FindCellByEventId(Guid eventId)
    {
        return Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Items.Any(e => e.Id == eventId));
    }

    public Cell<TEvent>? FindCellById(Guid cellId)
    {
        return Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Id == cellId);
    }
}
