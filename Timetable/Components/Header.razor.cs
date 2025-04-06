using Microsoft.AspNetCore.Components;
using System.Globalization;
using Timetable.Common.Enums;
using Timetable.Common.Utilities;
using Timetable.Configuration;

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
            DisplayType.Week => GetWeekHeaderTitle(),
            DisplayType.Month => $"{Config.CurrentDate:MMMM yyyy}",
            _ => throw new NotImplementedException()
        };
    }

    private string GetWeekHeaderTitle()
    {
        var startOfWeek = DateHelper.GetStartOfWeekDate(Config.CurrentDate, Config.FirstDayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return $"{startOfWeek:dd MMMM yyyy} - {endOfWeek:dd MMMM yyyy}";
    }
}