namespace Timetable.Structure;

internal sealed class Cell<TEvent> where TEvent : class
{
    public Guid Id { get; init; }
    public DateTime Time { get; init; }
    public IList<EventWrapper<TEvent>> Events { get; set; } = [];
}
