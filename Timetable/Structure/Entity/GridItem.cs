namespace Timetable.Structure.Entity;

internal sealed class GridItem<TEvent> where TEvent : class
{
    public Guid Id { get; set; }
    public int Span { get; set; }
    public bool IsWholeDay { get; set; }
    public TEvent Event { get; set; } = default!;
}