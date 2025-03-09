namespace Timetable.Structure;

internal sealed class GridCell<TEvent> where TEvent : class
{
    public Guid Id { get; init; }
    public DateTime CellTime { get; init; }
    public IList<GridEvent<TEvent>> Events { get; set; } = [];
}
