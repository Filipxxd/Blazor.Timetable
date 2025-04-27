using System.Linq.Expressions;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Components.Shared.Modals;
using Timetable.Configuration;

namespace Timetable.Models;
public class AdditionalFieldInfo<TEvent>
{
    // The selector expression (e.g. x => x.Description)
    public Expression<Func<TEvent, object?>> Selector { get; set; } = default!;
    // Optionally, you could store a property name for convenience.
    public string PropertyName { get; set; } = string.Empty;
}
internal sealed class TimetableManager<TEvent> where
    TEvent : class
{
    public required CompiledProps<TEvent> Props { get; init; }
    public IList<TEvent> Events { get; set; } = [];

    public Grid<TEvent> Grid { get; set; } = default!;
    public DateOnly CurrentDate { get; set; }
    public DisplayType DisplayType { get; set; }

    public void NextDate(TimetableConfig config)
    {
        CurrentDate = CurrentDate.GetValidDate(DisplayType, false, config.Days, config.Months);
    }

    public void PreviousDate(TimetableConfig config)
    {
        CurrentDate = CurrentDate.GetValidDate(DisplayType, true, config.Days, config.Months);
    }

    public TEvent? MoveEvent(Guid eventId, Guid targetCellId)
    {
        var currentCell = FindCellByEventId(eventId);
        if (currentCell is null)
            return null;

        var timetableEvent = currentCell.Events.FirstOrDefault(e => e.Id == eventId);
        if (timetableEvent is null)
            return null;

        var targetCell = FindCellById(targetCellId);
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

    public TEvent UpdateEvent(UpdateProps<TEvent> props)
    {
        var newEvent = props.EventWrapper;

        var timetableEvent = FindCellByEventId(newEvent.Id).Events.FirstOrDefault(e => e.Id == newEvent.Id);
        if (timetableEvent is null)
            return newEvent.Event;

        timetableEvent.Title = newEvent.Title;
        timetableEvent.DateFrom = newEvent.DateFrom;
        timetableEvent.DateTo = newEvent.DateTo;
        timetableEvent.GroupIdentifier = newEvent.GroupIdentifier;

        foreach (var field in props.AdditionalFieldInfos)
        {
            // Use reflection to copy the value from the original event to the deep copy.
            var prop = GetPropertyInfo(field.Selector);
            if (prop != null && prop.CanRead && prop.CanWrite)
            {
                var originalValue = prop.GetValue(newEvent.Event);
                prop.SetValue(timetableEvent, originalValue);
            }
        }
        // TODO: other props
        return timetableEvent.Event;
    }
    private System.Reflection.PropertyInfo? GetPropertyInfo(Expression selector)
    {
        if (selector is LambdaExpression lambda &&
            lambda.Body is MemberExpression member &&
            member.Member is System.Reflection.PropertyInfo prop)
        {
            return prop;
        }
        return null;
    }
    public IList<TEvent>? ChangeGroupEvent(Guid eventId, bool futureOnly = false)
    {
        if (Props.GetGroupId is null) return null;

        var timetableEvent = FindCellByEventId(eventId).Events.FirstOrDefault(e => e.Id == eventId);

        if (timetableEvent is null || timetableEvent.GroupIdentifier is null) return null;

        var groupId = timetableEvent.GroupIdentifier;

        var relatedEventsToUpdate = Events
            .Where(e =>
            {
                var groupId = Props.GetGroupId(e);
                return groupId?.Equals(groupId) == true && groupId != timetableEvent.GroupIdentifier;
            });

        if (futureOnly)
        {
            relatedEventsToUpdate = [.. relatedEventsToUpdate.Where(e => Props.GetDateFrom(e).ToDateOnly() >= CurrentDate)];
        }

        var updatedEvents = new List<TEvent> { timetableEvent.Event };

        foreach (var evt in relatedEventsToUpdate)
        {
            var groupIdentifier = Props.GetGroupId(evt);

            if (groupIdentifier is null || !groupIdentifier.Equals(groupId)) continue;

            var originalDateFrom = Props.GetDateFrom(evt);
            var originalDateTo = Props.GetDateTo(evt);
            var duration = originalDateTo - originalDateFrom;

            var newDateFrom = timetableEvent.DateFrom;
            var newDateTo = newDateFrom.Add(duration);

            Props.SetTitle(evt, timetableEvent.Title);
            Props.SetDateFrom(evt, newDateFrom);
            Props.SetDateTo(evt, newDateTo);

            updatedEvents.Add(evt);
        }

        return updatedEvents;
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
            // TODO
            relatedEventsToUpdate = [.. relatedEventsToUpdate.Where(e => Props.GetDateFrom(e).ToDateOnly() >= CurrentDate)];
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
        return Grid.Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Events.Any(e => e.Id == eventId));
    }

    private Cell<TEvent>? FindCellById(Guid cellId)
    {
        return Grid.Columns.SelectMany(col => col.Cells)
                           .FirstOrDefault(cell => cell.Id == cellId);
    }
}
