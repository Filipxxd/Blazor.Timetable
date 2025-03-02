using System.Globalization;
using Microsoft.AspNetCore.Components;
using Timetable.Configuration;
using Timetable.Enums;

namespace Timetable.Components;

public partial class Header : ComponentBase
{
    [Parameter] public TimetableConfig Config { get; set; } = new();
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
    
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