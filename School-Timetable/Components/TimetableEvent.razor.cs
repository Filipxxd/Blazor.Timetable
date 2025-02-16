using Microsoft.AspNetCore.Components;

namespace School_Timetable.Components;

public partial class TimetableEvent
{
    private bool _popoverVisible = false;
    
    [Parameter] public Guid EventId { get; set; }
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public RenderFragment DetailTemplate { get; set; } = default!; 
    
    private void TogglePopover()
    {
        _popoverVisible = !_popoverVisible;
    }
}