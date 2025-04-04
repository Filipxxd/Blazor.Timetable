namespace Timetable.Structure;

internal sealed class Row<TEvent> where TEvent : class
{
    public DateTime StartTime { get; set; }
    public IList<Cell<TEvent>> Cells { get; set; } = [];
}
