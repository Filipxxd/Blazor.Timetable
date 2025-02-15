namespace School_Timetable.Structure.Entity;

public sealed class GridItem<TEvent> where TEvent : class
{
    public Guid Id { get; init; }
    public TEvent EventDetail { get; init; } = default!;
}