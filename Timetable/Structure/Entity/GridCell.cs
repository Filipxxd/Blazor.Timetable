namespace Timetable.Structure.Entity;

internal sealed class GridCell<TEvent> where TEvent : class
{
    public Guid Id { get; init; }
    public DateTime CellTime { get; init; }
    public IList<GridItem<TEvent>> Events { get; set; } = [];
}
