using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Models.Props;

namespace Timetable.Models;

internal sealed class TimetableManager<TEvent> where
    TEvent : class
{
    public required CompiledProps<TEvent> Props { get; init; }

    public Grid<TEvent> Grid { get; set; } = default!;
    public DateOnly OriginalDate { get; set; }
    public DateOnly CurrentDate { get; set; }
    public DisplayType DisplayType { get; set; }

    public IList<TEvent> DeleteEvent(IList<TEvent> events, DeleteProps<TEvent> deleteProps)
    {
        if (deleteProps.Scope == ActionScope.Current)
        {
            events.Remove(deleteProps.EventWrapper.Event);
            return [deleteProps.EventWrapper.Event];
        }

        if (deleteProps.EventWrapper.GroupIdentifier is null)
            throw new InvalidOperationException("Cannot delete grouped events without group identifier.");


        var groupId = deleteProps.EventWrapper.GroupIdentifier;

        var relatedEvents = events.Where(e =>
        {
            var eGroup = deleteProps.EventWrapper.Props.GetGroupId(e);
            return eGroup != null && eGroup.Equals(groupId);
        }).ToList();

        foreach (var relatedEvent in relatedEvents)
        {
            events.Remove(relatedEvent);
        }

        return relatedEvents;
    }

    public TEvent? MoveEvent(Guid eventId, Guid targetCellId)
    {
        var currentCell = Grid.FindCellByEventId(eventId);
        if (currentCell is null)
            return null;

        var timetableEvent = currentCell.Events.FirstOrDefault(e => e.Id == eventId);
        if (timetableEvent is null)
            return null;

        var targetCell = Grid.FindCellById(targetCellId);
        if (targetCell is null)
            return null;

        if (currentCell.Type != targetCell.Type || targetCell.Type == CellType.Disabled)
            return null;

        if (currentCell.Type == CellType.Normal)
        {
            var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
            var newEndDate = targetCell.DateTime.Add(duration);

            timetableEvent.DateFrom = targetCell.DateTime;
            timetableEvent.DateTo = newEndDate;

            currentCell.Events.Remove(timetableEvent);
            targetCell.Events.Add(timetableEvent);
        }
        else
        {
            var duration = timetableEvent.DateTo - timetableEvent.DateFrom;
            var originalFrom = timetableEvent.DateFrom;
            var newStartDate = new DateTime(targetCell.DateTime.Year, targetCell.DateTime.Month, targetCell.DateTime.Day, originalFrom.Hour, originalFrom.Minute, originalFrom.Second);
            var newEndDate = newStartDate.Add(duration);

            timetableEvent.DateFrom = newStartDate;
            timetableEvent.DateTo = newEndDate;
            currentCell.Events.Remove(timetableEvent);
            targetCell.Events.Add(timetableEvent);
        }

        return timetableEvent.Event;
    }

    public IList<TEvent> UpdateEvents(IList<TEvent> events, UpdateProps<TEvent> props)
    {
        var originalGroup = props.Original.GroupIdentifier;
        if (originalGroup is null)
            throw new InvalidOperationException("Cannot update grouped events without group identifier.");

        var originalDateFrom = props.Original.Props.GetDateFrom(props.Original.Event);
        var originalDateTo = props.Original.Props.GetDateTo(props.Original.Event);
        var newDateFromCore = props.Original.Props.GetDateFrom(props.New.Event);
        var newDateToCore = props.Original.Props.GetDateTo(props.New.Event);

        var startOffset = newDateFromCore - originalDateFrom;
        var endOffset = newDateToCore - originalDateTo;
        var newTitle = props.Original.Props.GetTitle(props.New.Event);

        var relatedEventsToUpdate = events.Where(e =>
        {
            var eGroup = props.Original.Props.GetGroupId(e);
            return eGroup != null && eGroup.Equals(originalGroup);
        });

        if (props.Scope == ActionScope.Future)
        {
            relatedEventsToUpdate = relatedEventsToUpdate.Where(e =>
                props.Original.Props.GetDateFrom(e).ToDateOnly() >= CurrentDate);
        }

        var updatedEvents = new List<TEvent>();
        foreach (var evt in relatedEventsToUpdate)
        {
            var eventOriginalFrom = props.Original.Props.GetDateFrom(evt);
            var eventOriginalTo = props.Original.Props.GetDateTo(evt);

            var newEventFrom = eventOriginalFrom + startOffset;
            var newEventTo = eventOriginalTo + endOffset;

            props.Original.Props.SetTitle(evt, newTitle);
            props.Original.Props.SetDateFrom(evt, newEventFrom);
            props.Original.Props.SetDateTo(evt, newEventTo);

            foreach (var (getter, setter) in props.New.Props.AdditionalProperties)
            {
                var updatedValue = getter(props.New.Event);
                setter(evt, updatedValue);
            }
            updatedEvents.Add(evt);
        }

        return updatedEvents;
    }

    public TEvent UpdateEvent(UpdateProps<TEvent> props)
    {
        if (props.Original.HasGroupdAssigned)
            props.New.GroupIdentifier = null;

        props.New.MapTo(props.Original.Event);

        return props.Original.Event;
    }
}
