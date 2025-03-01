using System.Globalization;
using Microsoft.AspNetCore.Components;
using School_Timetable.Configuration;
using School_Timetable.Enums;

namespace School_Timetable.Components;

public partial class TimetableOptions
{
    [Parameter] public TimetableConfig Config { get; set; } = new();
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }

    private void HandleDisplayTypeChanged(DisplayType displayType)
    {
        Config.DisplayType = displayType;
        OnDisplayTypeChanged.InvokeAsync(displayType);
    }
    
    private string GetHeaderTitle()
    {
        return Config.DisplayType switch
        {
            DisplayType.Day => Config.CurrentDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture),
            DisplayType.Week => $"{Config.CurrentDate:dd MMMM yyyy} - {Config.CurrentDate.AddDays(6):dd MMMM yyyy}",
            DisplayType.Month => $"{Config.CurrentDate:MMMM yyyy}",
            _ => throw new NotImplementedException()
        };
    }
}