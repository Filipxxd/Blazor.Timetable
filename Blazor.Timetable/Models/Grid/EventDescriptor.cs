namespace Blazor.Timetable.Models.Grid;

public sealed class EventDescriptor<TEvent> where
    TEvent : class
{
    public EventDescriptor(TEvent timetableEvent, PropertyAccessors<TEvent> props)
    {
        Event = timetableEvent;
        Props = props;
    }

    public PropertyAccessors<TEvent> Props { get; init; }
    public TEvent Event { get; init; }

    public bool HasGroupdAssigned => GroupId != null;

    public string Title
    {
        get => Props.GetTitle(Event);
        set => Props.SetTitle(Event, value);
    }
    public DateTime DateFrom
    {
        get => Props.GetDateFrom(Event);
        set => Props.SetDateFrom(Event, value);
    }
    public DateTime DateTo
    {
        get => Props.GetDateTo(Event);
        set => Props.SetDateTo(Event, value);
    }
    public string? GroupId
    {
        get => Props?.GetGroupId != null ? Props.GetGroupId(Event) : null;
        set
        {
            if (Props?.SetGroupId != null)
            {
                Props.SetGroupId(Event, value);
            }
        }
    }

    public EventDescriptor<TEvent> Copy()
    {
        var eventCopy = Activator.CreateInstance<TEvent>();

        Props.SetTitle(eventCopy, Title);
        Props.SetDateFrom(eventCopy, DateFrom);
        Props.SetDateTo(eventCopy, DateTo);
        Props.SetGroupId(eventCopy, GroupId);

        foreach (var (getter, setter) in Props.AdditionalProperties)
        {
            var originalValue = getter(Event);
            setter(eventCopy, originalValue);
        }

        return new EventDescriptor<TEvent>(eventCopy, Props);
    }

    public TEvent MapTo(TEvent timetableEvent)
    {
        Props.SetTitle(timetableEvent, Title);
        Props.SetDateFrom(timetableEvent, DateFrom);
        Props.SetDateTo(timetableEvent, DateTo);
        Props.SetGroupId(timetableEvent, GroupId);

        foreach (var (getter, setter) in Props.AdditionalProperties)
        {
            var originalValue = getter(Event);
            setter(timetableEvent, originalValue);
        }

        return timetableEvent;
    }
}
