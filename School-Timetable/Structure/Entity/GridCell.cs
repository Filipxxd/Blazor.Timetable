namespace School_Timetable.Structure.Entity;

internal class GridCell<T> where T : class
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CellTime { get; set; }
    public IList<T> Events { get; set; } = [];
}
