namespace Blazor.Timetable.Models.Grid;

public sealed class CellItem<TEvent> where TEvent : class
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int Span { get; init; }
    public EventWrapper<TEvent> EventWrapper { get; init; } = default!;
}
