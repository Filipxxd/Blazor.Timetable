using Microsoft.AspNetCore.Components;
using Timetable.Models.Configuration;

namespace Timetable.Components;

public partial class Header
{
    [CascadingParameter] public TimetableConfig Config { get; set; } = default!;
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
}