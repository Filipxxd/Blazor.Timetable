using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace School_Timetable.Components;

public partial class TimetableOptions
{
    [Parameter] public IEnumerable<DisplayType> SupportedDisplayTypes { get; set; } = [];
    [Parameter] public DisplayType SelectedDisplayType { get; set; }
    [Parameter] public EventCallback<DisplayType> SelectedDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback OnNextClicked { get; set; }
    [Parameter] public EventCallback OnPreviousClicked { get; set; }
    [Parameter] public DateTime CurrentDate { get; set; }

    private string GetHeaderTitle()
    {
        var displayType = SelectedDisplayType;
        return displayType switch
        {
            DisplayType.Day => CurrentDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture),
            DisplayType.Week => $"{CurrentDate:dd MMMM yyyy} - {CurrentDate.AddDays(6):dd MMMM yyyy}",
            DisplayType.Month => $"{CurrentDate:MMMM yyyy}",
            _ => throw new NotImplementedException()
        };
    }
}