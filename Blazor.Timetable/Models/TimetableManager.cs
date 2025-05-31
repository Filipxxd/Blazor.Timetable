using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Models;

internal sealed class TimetableManager<TEvent> where
    TEvent : class
{
    public PropertyAccessors<TEvent> Props { get; init; }

    public Grid<TEvent> Grid { get; set; } = default!;
    public DateOnly CurrentDate { get; set; } = DateTime.Now.ToDateOnly();
    public DisplayType DisplayType { get; set; }

    public TimetableManager(PropertyAccessors<TEvent> props)
    {
        Props = props;
    }

    public IList<TEvent> CreateEvents(
            EventDescriptor<TEvent> templateDescriptor,
            Repeatability repetition,
            DateOnly? repeatUntil,
            int? repeatDays = null)
    {
        var rootDesc = templateDescriptor.DeepCopy();
        var result = new List<EventDescriptor<TEvent>> { rootDesc };

        if (repetition != Repeatability.Once)
        {
            var groupId = Guid.NewGuid().ToString();
            rootDesc.GroupId = groupId;

            var baseFrom = rootDesc.DateFrom;
            var baseTo = rootDesc.DateTo;
            var i = 1;

            while (true)
            {
                var nextFrom = repetition switch
                {
                    Repeatability.Daily => baseFrom.AddDays(i),
                    Repeatability.Weekly => baseFrom.AddDays(7 * i),
                    Repeatability.Monthly => baseFrom.AddMonths(i),
                    Repeatability.Custom when repeatDays.HasValue => baseFrom.AddDays(repeatDays.Value * i),
                    _ => throw new ArgumentException("Invalid repetition"),
                };

                if (nextFrom.ToDateOnly() > repeatUntil)
                    break;

                var desc = rootDesc.DeepCopy();
                desc.DateFrom = nextFrom;
                desc.DateTo = baseTo + (nextFrom - baseFrom);
                desc.GroupId = groupId;
                result.Add(desc);
                i++;
            }
        }

        return [.. result.Select(d => d.Event)];
    }

    public TEvent? MoveEvent(Guid cellItemId, Guid targetCellId)
    {
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

        cellItem.EventDescriptor.GroupId = null;

        return cellItem.EventDescriptor.Event;
    }

    public IList<TEvent>? MoveEventGroup(IList<TEvent> events, Guid cellItemId, Guid targetCellId)
    {
        var currentCell = Grid.FindCellByEventId(cellItemId);
        if (currentCell is null)
            return null;

        var cellItem = currentCell.Items.FirstOrDefault(cellItem => cellItem.Id == cellItemId);
        if (cellItem is null)
            return null;

        var targetCell = Grid.FindCellById(targetCellId);
        if (targetCell is null || currentCell.Type != targetCell.Type || targetCell.Type == CellType.Disabled)
            return null;

        var groupId = cellItem.EventDescriptor.GroupId;
        if (groupId is null)
            return null;

        var relatedEvents = events.Where(e =>
            groupId.Equals(Props.GetGroupId(e))
        ).ToList();

        if (relatedEvents.Count == 0)
            return null;

        var originalDateFrom = cellItem.EventDescriptor.DateFrom;
        var offset = targetCell.DateTime - originalDateFrom;

        foreach (var eventItem in relatedEvents)
        {
            var eventFrom = Props.GetDateFrom(eventItem);
            var eventTo = Props.GetDateTo(eventItem);

            var newEventFrom = eventFrom + offset;
            var newEventTo = eventTo + offset;

            Props.SetDateFrom(eventItem, newEventFrom);
            Props.SetDateTo(eventItem, newEventTo);
        }

        return relatedEvents;
    }

    public TEvent UpdateEvent(UpdateAction<TEvent> props)
    {
        if (props.Original.HasGroupdAssigned)
            props.New.GroupId = null;

        props.New.MapTo(props.Original.Event);

        return props.Original.Event;
    }

    public IList<TEvent> UpdateGroupEvent(IList<TEvent> events, UpdateAction<TEvent> props)
    {
        var originalGroup = props.Original.GroupId
            ?? throw new InvalidOperationException("Cannot update grouped events without group identifier.");

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
            relatedEventsToUpdate = relatedEventsToUpdate.Where(e => props.Original.Props.GetDateFrom(e).ToDateOnly() >= CurrentDate);

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

    public TEvent DeleteEvent(IList<TEvent> events, DeleteAction<TEvent> deleteProps)
    {
        events.Remove(deleteProps.EventDescriptor.Event);
        return deleteProps.EventDescriptor.Event;
    }

    public IList<TEvent> DeleteGroupEvent(IList<TEvent> events, DeleteAction<TEvent> deleteProps)
    {
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
}
