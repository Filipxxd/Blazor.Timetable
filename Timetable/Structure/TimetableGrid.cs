namespace Timetable.Structure;

internal sealed class TimetableGrid<TEvent> where TEvent : class
{
    public EventProps<TEvent> EventProps { get; set; } = default!;
    public IList<TEvent> SourceEvents { get; init; } = [];
    public Row<TEvent> HeaderRow { get; set; } = new();
    public IList<Row<TEvent>> Rows { get; set; } = [];
    // TODO: Prop for additional row/col (daily/weekly/monthly)

    public bool TryMoveEvent(Guid eventId, Guid targetCellId, out TEvent? movedEvent)
    {
        movedEvent = null;

        var timetableEvent = FindEventById(eventId);
        var targetCell = FindCellById(targetCellId);

        if (targetCell is null || timetableEvent is null) return false;

        var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
        var newEndDate = targetCell.Time.Add(duration);

        timetableEvent.DateFrom = targetCell.Time;
        timetableEvent.DateTo = newEndDate;

        var currentCell = FindCellByEventId(timetableEvent.Id);
        if (currentCell is null) return false;

        currentCell.Events.Remove(timetableEvent);
        targetCell.Events.Add(timetableEvent);
        movedEvent = timetableEvent.Event;
        return true;
    }

    public bool TryMoveGroupEvent(Guid eventId, Guid targetCellId, out IList<TEvent>? movedEvents)
    {
        movedEvents = null;

        var timetableEvent = FindEventById(eventId);
        var targetCell = FindCellById(targetCellId);

        if (targetCell is null || timetableEvent is null || timetableEvent.GroupIdentifier is null) return false;

        var groupEvents = FindEventsByGroupIdentifier(timetableEvent.GroupIdentifier);
        if (!groupEvents.Any()) return false;

        var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
        var timeDifference = targetCell.Time - timetableEvent.DateFrom;

        foreach (var groupEvent in groupEvents)
        {
            var newStartDate = groupEvent.DateFrom.Add(timeDifference);
            var newEndDate = newStartDate.Add(duration);

            groupEvent.DateFrom = newStartDate;
            groupEvent.DateTo = newEndDate;

            var currentCell = FindCellByEventId(groupEvent.Id);
            if (currentCell is null) continue;

            currentCell.Events.Remove(groupEvent);
            var newTargetCell = FindCellByTime(newStartDate);
            newTargetCell?.Events.Add(groupEvent);
        }

        var sourceGroupEvents = SourceEvents
            .Where(e => EventProps.GetGroupId?.Invoke(e)?.Equals(timetableEvent.GroupIdentifier) == true)
            .ToList();

        foreach (var sourceEvent in sourceGroupEvents)
        {
            var newStartDate = EventProps.GetDateFrom(sourceEvent).Add(timeDifference);
            var newEndDate = newStartDate.Add(duration);

            EventProps.SetDateFrom(sourceEvent, newStartDate);
            EventProps.SetDateTo(sourceEvent, newEndDate);
        }

        movedEvents = [.. groupEvents.Select(e => e.Event)];
        return true;
    }

    private IEnumerable<EventWrapper<TEvent>> FindEventsByGroupIdentifier(object groupIdentifier) =>
       Rows.SelectMany(row => row.Cells)
           .SelectMany(cell => cell.Events)
           .Where(item => item.GroupIdentifier?.Equals(groupIdentifier) == true);

    private Cell<TEvent>? FindCellByTime(DateTime time) =>
        Rows.SelectMany(row => row.Cells)
            .FirstOrDefault(cell => cell.Time == time);

    private Cell<TEvent>? FindCellById(Guid cellId) =>
        Rows.SelectMany(row => row.Cells).SingleOrDefault(cell => cell.Id == cellId);

    private EventWrapper<TEvent>? FindEventById(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .SelectMany(cell => cell.Events)
            .SingleOrDefault(item => item.Id == itemId);

    private Cell<TEvent>? FindCellByEventId(Guid itemId) =>
        Rows.SelectMany(row => row.Cells)
            .FirstOrDefault(cell => cell.Events.Any(x => x.Id == itemId));
}
