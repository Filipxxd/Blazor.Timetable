using Timetable.Common.Enums;
using Timetable.Common.Helpers;
using Timetable.Configuration;

namespace Timetable.Models;

internal sealed class TimetableManager<TEvent> where
    TEvent : class
{
    public required CompiledProps<TEvent> Props { get; init; }
    public IList<TEvent> Events { get; init; } = [];

    public Grid<TEvent> Grid { get; set; } = default!;
    public DateTime CurrentDate { get; set; }
    public DisplayType DisplayType { get; set; }

    public void NextDate(TimetableConfig config)
    {
        var increment = DateHelper.GetIncrement(DisplayType);
        CurrentDate = DateHelper.GetNextAvailableDate(CurrentDate, increment, config.Days);
    }

    public void PreviousDate(TimetableConfig config)
    {
        var increment = DateHelper.GetIncrement(DisplayType);
        CurrentDate = DateHelper.GetNextAvailableDate(CurrentDate, -increment, config.Days);
    }

    public TEvent? MoveEvent(Guid eventId, Guid targetCellId)
    {
        var currentCell = FindCellByEventId(eventId);
        if (currentCell is null) return null;

        var timetableEvent = currentCell.Events.FirstOrDefault(e => e.Id == eventId);
        if (timetableEvent is null) return null;

        var targetCell = FindCellById(targetCellId);
        if (targetCell is null || targetCell.Type != CellType.Normal) return null;

        var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
        var newEndDate = targetCell.DateTime.Add(duration);

        timetableEvent.DateFrom = targetCell.DateTime;
        timetableEvent.DateTo = newEndDate;

        currentCell.Events.Remove(timetableEvent);
        targetCell.Events.Add(timetableEvent);

        return timetableEvent.Event;
    }

    public IList<TEvent>? MoveGroupEvents(Guid eventId, Guid targetCellId, bool futureOnly = false)
    {
        if (Props.GetGroupId is null) return null;

        var currentCell = FindCellByEventId(eventId);
        if (currentCell is null) return null;

        var timetableEvent = currentCell.Events.FirstOrDefault(e => e.Id == eventId);
        if (timetableEvent is null || timetableEvent.GroupIdentifier is null) return null;

        var groupId = timetableEvent.GroupIdentifier;

        var targetCell = FindCellById(targetCellId);
        if (targetCell is null) return null;

        var relatedEventsToUpdate = Events
            .Where(e =>
            {
                var groupId = Props.GetGroupId(e);
                return groupId?.Equals(groupId) == true && groupId != timetableEvent.GroupIdentifier;
            });

        if (futureOnly)
        {
            relatedEventsToUpdate = [.. relatedEventsToUpdate.Where(e => Props.GetDateFrom(e) > CurrentDate)];
        }

        var updatedEvents = new List<TEvent> { timetableEvent.Event };

        foreach (var evt in relatedEventsToUpdate)
        {
            var groupIdentifier = Props.GetGroupId(evt);

            if (groupIdentifier is null || !groupIdentifier.Equals(groupId)) continue;

            var originalDateFrom = Props.GetDateFrom(evt);
            var originalDateTo = Props.GetDateTo(evt);
            var duration = originalDateTo - originalDateFrom;

            var newDateFrom = targetCell.DateTime;
            var newDateTo = newDateFrom.Add(duration);

            Props.SetDateFrom(evt, newDateFrom);
            Props.SetDateTo(evt, newDateTo);

            updatedEvents.Add(evt);
        }

        return updatedEvents;
    }

    private Cell<TEvent>? FindCellByEventId(Guid eventId)
    {
        return Grid.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type != CellType.Header))
                           .FirstOrDefault(cell => cell.Events.Any(e => e.Id == eventId));
    }

    private Cell<TEvent>? FindCellById(Guid cellId)
    {
        return Grid.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type != CellType.Header))
                           .FirstOrDefault(cell => cell.Id == cellId);
    }
}
