namespace Timetable.Structure;

internal sealed class GridRow<TEvent> where TEvent : class
{
    public DateTime RowStartTime { get; set; }
    public bool IsWholeDayRow { get; set; }
    public IList<GridCell<TEvent>> Cells { get; set; } = [];
}
