namespace School_Timetable.Structure.Entity;

public sealed class GridItem<TEvent> where TEvent : class
{
    public Guid Id { get; set; }
    public int Span { get; set; }
    public bool IsWholeDay { get; set; }
    public TEvent Event { get; set; } = default!;
}