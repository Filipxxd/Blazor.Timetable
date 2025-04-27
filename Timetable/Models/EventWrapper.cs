namespace Timetable.Models;

public sealed class EventWrapper<TEvent> where
    TEvent : class
{
    public required CompiledProps<TEvent> Props { get; init; }

    public Guid Id { get; init; } = Guid.NewGuid();
    public required TEvent Event { get; init; }
    public required int Span { get; init; }

    public bool HasGroupdAssigned => GroupIdentifier != null;

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
    public object? GroupIdentifier
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

    public EventWrapper<TEvent> Copy()
    {
        var eventCopy = Activator.CreateInstance<TEvent>();

        Props.SetTitle(eventCopy, Title);
        Props.SetDateFrom(eventCopy, DateFrom);
        Props.SetDateTo(eventCopy, DateTo);
        Props.SetGroupId(eventCopy, GroupIdentifier);

        foreach (var (getter, setter) in Props.AdditionalProperties)
        {
            var originalValue = getter(Event);
            setter(eventCopy, originalValue);
        }

        return new EventWrapper<TEvent>()
        {
            Props = Props,
            Event = eventCopy,
            Span = 0,
            Id = Id
        };
    }

    public TEvent MapTo(TEvent timetableEvent)
    {
        Props.SetTitle(timetableEvent, Title);
        Props.SetDateFrom(timetableEvent, DateFrom);
        Props.SetDateTo(timetableEvent, DateTo);
        Props.SetGroupId(timetableEvent, GroupIdentifier);

        foreach (var (getter, setter) in Props.AdditionalProperties)
        {
            var originalValue = getter(Event);
            setter(timetableEvent, originalValue);
        }

        return timetableEvent;
    }
}
