namespace Timetable.Structure;

internal sealed class Cell<TEvent> where
    TEvent : class
{
    public required Guid Id { get; init; }
    public required DateTime DateTime { get; init; }
    public string? Title { get; set; }
    public required int ColumnIndex { get; init; }
    public required int RowIndex { get; init; }
    public required bool IsHeaderCell { get; init; }
    public List<EventWrapper<TEvent>> Events { get; init; } = [];
}
