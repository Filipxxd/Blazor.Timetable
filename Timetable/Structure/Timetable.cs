namespace Timetable.Structure;

internal sealed class Timetable<TEvent> where TEvent : class
{
    public IList<GridRow<TEvent>> Rows { get; set; } = [];
    // TODO: Prop for additional row/col (daily/weekly/monthly)
    
    public bool TryMoveEvent(Guid eventId, Guid targetCellId, out TEvent? movedEvent)
    {
        // TODO: Add group event logic (modal to confirm either group update or single update)
        movedEvent = null;

        var timetableEvent = FindEventById(eventId);
        var targetCell = FindCellById(targetCellId);

        if (targetCell is null || timetableEvent is null) return false;
        
        var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
        var newEndDate = targetCell.CellTime.Add(duration);

        timetableEvent.DateFrom = targetCell.CellTime;
        timetableEvent.DateTo = newEndDate;

        var currentCell = FindCellByEventId(timetableEvent.Id);
        if (currentCell is null) return false;

        currentCell.Events.Remove(timetableEvent);
        targetCell.Events.Add(timetableEvent);
        movedEvent = timetableEvent.Event;
        return true;
    }

    private GridCell<TEvent>? FindCellById(Guid cellId) =>
        Rows.SelectMany(row => row.Cells).SingleOrDefault(cell => cell.Id == cellId);

    private GridEvent<TEvent>? FindEventById(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .SelectMany(cell => cell.Events)
            .SingleOrDefault(item => item.Id == itemId);

    private GridCell<TEvent>? FindCellByEventId(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .FirstOrDefault(cell => cell.Events.Any(x => x.Id == itemId));
}
