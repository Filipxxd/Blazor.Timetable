using Timetable.Configuration;

namespace Timetable.Structure;

internal sealed class EventWrapper<TEvent> where TEvent : class
{
    private readonly EventProps<TEvent> _props;
    private readonly TimetableConfig _config;

    public EventWrapper(TEvent timetableEvent, EventProps<TEvent> props, TimetableConfig config)
    {
        Event = timetableEvent;
        _props = props;
        _config = config;

        Id = Guid.NewGuid();
    }

    public string? Title
    {
        get => string.IsNullOrWhiteSpace(_props.GetTitle(Event)?.Trim())
            ? "Unknown Event"
            : _props.GetTitle(Event);
        set => _props.SetTitle(Event, value);
    }
    public DateTime DateFrom
    {
        get => _props.GetDateFrom(Event);
        set => _props.SetDateFrom(Event, value);
    }
    public DateTime DateTo
    {
        get => _props.GetDateTo(Event);
        set => _props.SetDateTo(Event, value);
    }
    public object? GroupIdentifier
    {
        get => _props?.GetGroupId != null ? _props.GetGroupId(Event) : null;
        set
        {
            if (_props?.SetGroupId != null)
            {
                _props.SetGroupId(Event, value);
            }
        }
    }

    public TEvent Event { get; }
    public Guid Id { get; }
    public bool IsHeaderEvent
        => (DateTo.Hour >= _config.TimeTo.Hour && DateFrom.Hour <= _config.TimeFrom.Hour) || DateFrom.Date != DateTo.Date;
    public int Span
    {
        get
        {
            var hours = (int)(DateTo - DateFrom).TotalHours;

            return IsHeaderEvent ? hours - 1 : hours;
        }
    }
}