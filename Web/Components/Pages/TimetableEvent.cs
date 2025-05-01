namespace Web.Components.Pages;

public class TimetableEvent
{
    public int Occupancy { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? GroupId { get; set; }

    public int? Id { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
