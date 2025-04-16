namespace Timetable.Structure;

internal sealed class EventWrapper<TEvent> where
    TEvent : class
{
    public required CompiledProps<TEvent> Props { get; init; }

    public required TEvent Event { get; init; }
    public required Guid Id { get; init; }
    public required int Span { get; init; } // if IsHeader -> span accross columns (multi day event) else span accross rows (mtuli hours)

    public bool HasGroupdAssigned => GroupIdentifier != null;

    public string Title
    {
        get => string.IsNullOrWhiteSpace(Props.GetTitle(Event)?.Trim())
            ? "Unknown Event"
            : Props.GetTitle(Event);
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
}
