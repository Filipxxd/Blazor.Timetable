using Microsoft.AspNetCore.Components;
using Blazor.Timetable.Models.Configuration;

namespace Blazor.Timetable.Components;

public partial class Header
{
    [CascadingParameter] public TimetableConfig Config { get; set; } = default!;
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
}