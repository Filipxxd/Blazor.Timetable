namespace School_Timetable.Structure.Entity;

internal class GridRow<T> where T : class
{
    public DateTime RowStartTime { get; set; }
    public IList<GridCell<T>> Cells { get; set; } = [];
}
