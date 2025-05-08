using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components;

public partial class Header
{
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
}