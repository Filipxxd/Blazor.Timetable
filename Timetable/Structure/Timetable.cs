using Timetable.Structure.Entity;

namespace Timetable.Structure;

internal sealed class Timetable<TEvent> where TEvent : class
{
    private readonly Func<TEvent, DateTime> _getDateFrom;
    private readonly Func<TEvent, DateTime> _getDateTo;
    private readonly Func<TEvent, string?> _getTitle;
    private readonly Func<TEvent, object?> _getGroupIdentifier;
    private readonly Action<TEvent, DateTime> _setDateFrom;
    private readonly Action<TEvent, DateTime> _setDateTo;
    private readonly Action<TEvent, object?> _setGroupIdentifier;

    public IList<GridRow<TEvent>> Rows { get; set; } = [];

    public Timetable(
        Func<TEvent, DateTime> getDateFrom,
        Func<TEvent, DateTime> getDateTo,
        Func<TEvent, string?> getTitle,
        Func<TEvent, object?> getGroupIdentifier,
        Action<TEvent, DateTime> setDateFrom,
        Action<TEvent, DateTime> setDateTo,
        Action<TEvent, object?> setGroupIdentifier)
    {
        _getDateFrom = getDateFrom;
        _getDateTo = getDateTo;
        _getTitle = getTitle;
        _getGroupIdentifier = getGroupIdentifier;
        _setDateFrom = setDateFrom;
        _setDateTo = setDateTo;
        _setGroupIdentifier = setGroupIdentifier;
    }

    public bool TryMoveEvent(Guid eventId, Guid targetCellId, out TEvent? movedEvent)
    {
        // TODO: Add group event logic (modal to confirm either group update or single update)
        movedEvent = null;

        var gridItem = FindItemById(eventId);
        var targetCell = FindCellById(targetCellId);

        if (targetCell is null || gridItem is null) return false;

        UpdateEventTiming(gridItem, targetCell.CellTime);

        var currentCell = FindCellByItemId(gridItem.Id);
        if (currentCell is null) return false;

        currentCell.Events.Remove(gridItem);
        targetCell.Events.Add(gridItem);
        movedEvent = gridItem.Event;
        return true;
    }

    private GridCell<TEvent>? FindCellById(Guid cellId) =>
        Rows.SelectMany(row => row.Cells).SingleOrDefault(cell => cell.Id == cellId);

    private GridItem<TEvent>? FindItemById(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .SelectMany(cell => cell.Events)
            .SingleOrDefault(item => item.Id == itemId);

    private void UpdateEventTiming(GridItem<TEvent> gridItem, DateTime newStartDate)
    {
        var oldDateFrom = _getDateFrom(gridItem.Event);
        var duration = _getDateTo(gridItem.Event) - oldDateFrom;
        var newEndDate = newStartDate.Add(duration);

        _setDateFrom(gridItem.Event, newStartDate);
        _setDateTo(gridItem.Event, newEndDate);
    }

    private GridCell<TEvent>? FindCellByItemId(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .FirstOrDefault(cell => cell.Events.Any(x => x.Id == itemId));
}
