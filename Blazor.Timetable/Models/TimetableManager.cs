using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Models.Props;

namespace Blazor.Timetable.Models;

internal sealed class TimetableManager<TEvent> where
    TEvent : class
{
    public required PropertyAccessors<TEvent> Props { get; init; }

    public Grid<TEvent> Grid { get; set; } = default!;
    public DateOnly CurrentDate { get; set; }
    public DisplayType DisplayType { get; set; }

    public IList<TEvent> DeleteEvent(IList<TEvent> events, DeleteProps<TEvent> deleteProps)
    {
        if (deleteProps.Scope == ActionScope.Single)
        {
            events.Remove(deleteProps.EventDescriptor.Event);
            return [deleteProps.EventDescriptor.Event];
        }

        if (!deleteProps.EventDescriptor.HasGroupdAssigned)
            throw new InvalidOperationException("Cannot delete grouped events without group identifier.");

        var groupId = deleteProps.EventDescriptor.GroupId;

        var relatedEvents = events.Where(e =>
        {
            var eGroup = deleteProps.EventDescriptor.Props.GetGroupId(e);
            return eGroup != null && eGroup.Equals(groupId);
        }).ToList();

        if (deleteProps.Scope == ActionScope.Future)
            relatedEvents = [.. relatedEvents.Where(e =>
            {
                var eventStart = deleteProps.EventDescriptor.Props.GetDateFrom(e);
                return eventStart >= deleteProps.EventDescriptor.DateFrom;
            })];

        foreach (var relatedEvent in relatedEvents)
        {
            events.Remove(relatedEvent);
        }

        return relatedEvents;
    }

    public TEvent? MoveEvent(Guid cellItemId, Guid targetCellId)
    {
        // TODO: Group move
        var currentCell = Grid.FindCellByEventId(cellItemId);
        if (currentCell is null)
            return null;

        var cellItem = currentCell.Items.FirstOrDefault(cellItem => cellItem.Id == cellItemId);
        if (cellItem is null)
            return null;

        var targetCell = Grid.FindCellById(targetCellId);
        if (targetCell is null)
            return null;

        if (currentCell.Type != targetCell.Type || targetCell.Type == CellType.Disabled)
            return null;

        if (currentCell.Type == CellType.Normal)
        {
            var duration = cellItem.EventDescriptor.DateTo - cellItem.EventDescriptor.DateFrom;
            var newEndDate = targetCell.DateTime.Add(duration);

            cellItem.EventDescriptor.DateFrom = targetCell.DateTime;
            cellItem.EventDescriptor.DateTo = newEndDate;

            currentCell.Items.Remove(cellItem);
            targetCell.Items.Add(cellItem);
        }
        else
        {
            var duration = cellItem.EventDescriptor.DateTo - cellItem.EventDescriptor.DateFrom;
            var originalFrom = cellItem.EventDescriptor.DateFrom;
            var newStartDate = new DateTime(targetCell.DateTime.Year, targetCell.DateTime.Month, targetCell.DateTime.Day, originalFrom.Hour, originalFrom.Minute, originalFrom.Second);
            var newEndDate = newStartDate.Add(duration);

            cellItem.EventDescriptor.DateFrom = newStartDate;
            cellItem.EventDescriptor.DateTo = newEndDate;
            currentCell.Items.Remove(cellItem);
            targetCell.Items.Add(cellItem);
        }

        return cellItem.EventDescriptor.Event;
    }

    public IList<TEvent> UpdateEvents(IList<TEvent> events, UpdateProps<TEvent> props)
    {
        var originalGroup = props.Original.GroupId;
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
            props.New.GroupId = null;

        props.New.MapTo(props.Original.Event);

        return props.Original.Event;
    }
}
