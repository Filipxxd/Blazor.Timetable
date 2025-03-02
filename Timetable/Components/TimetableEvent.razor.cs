using Microsoft.AspNetCore.Components;

namespace Timetable.Components;

public partial class TimetableEvent
{
    private bool _popoverVisible = false;
    
    [Parameter] public Guid EventId { get; set; }
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public int Span { get; set; }
    [Parameter] public bool IsWholeDay { get; set; }
    [Parameter] public RenderFragment DetailTemplate { get; set; } = default!; 
    
    private void TogglePopover()
    {
        _popoverVisible = !_popoverVisible;
    }
}