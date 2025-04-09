using Microsoft.AspNetCore.Components;
using Timetable.Configuration;

namespace Timetable.Components;

public partial class Header : ComponentBase
{
    [Parameter, EditorRequired] public TimetableConfig Config { get; set; } = default!;
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
}