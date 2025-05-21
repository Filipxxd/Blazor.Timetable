namespace Web.Components.Pages;

public class Subject
{
    public string Name { get; set; } = string.Empty;
    public int Occupancy { get; set; } = 1;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? GroupId { get; set; }

    public int? Id { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
}
