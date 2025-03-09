using Timetable.Configuration;

namespace Timetable.Structure;

internal sealed class GridEvent<TEvent> where TEvent : class
{
    private readonly TimetableEventProps<TEvent> _props;
    private readonly TimetableConfig _config;

    public GridEvent(TEvent @event, TimetableEventProps<TEvent> props, TimetableConfig config)
    {
        Event = @event;
        _props = props;
        _config = config;

        Id = Guid.NewGuid();
    }

    public string? Title
    {
        get => _props.GetTitle(Event);
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
        get => _props.GetGroupId(Event);
        set => _props.SetGroupId(Event, value);
    }
    
    public TEvent Event { get; }
    public Guid Id { get; }
    public bool IsWholeDay => DateTo.Hour >= _config.TimeTo.Hour && DateFrom.Hour <= _config.TimeFrom.Hour;
    public int Span => (int)(DateTo - DateFrom).TotalHours;
}