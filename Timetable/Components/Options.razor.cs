using Microsoft.AspNetCore.Components;
using Timetable.Configuration;
using Timetable.Enums;

namespace Timetable.Components;

public partial class Options : ComponentBase
{
    [Parameter] public TimetableConfig Config { get; set; } = new();
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    
    private void HandleDisplayTypeChanged(DisplayType displayType)
    {
        Config.DisplayType = displayType;
        OnDisplayTypeChanged.InvokeAsync(displayType);
    }
}