namespace Timetable.Structure;

internal sealed class Cell<TEvent> where
    TEvent : class
{
    public required Guid Id { get; init; }
    public required DateTime DateTime { get; init; }
    public string? Title { get; set; }
    public List<EventWrapper<TEvent>> Events { get; init; } = [];
}
