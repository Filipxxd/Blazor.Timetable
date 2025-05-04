using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazor.Timetable.Components.Shared.Forms;

public partial class InputDateTime<TEvent> : BaseInput<TEvent, DateTime>
{
    [Parameter] public InputDateType Type { get; set; } = InputDateType.DateTimeLocal;

    private DateTime DatePart
    {
        get => BindProperty.Date;
        set => BindProperty = new DateTime(value.Year, value.Month, value.Day, BindProperty.Hour, BindProperty.Minute, BindProperty.Second);
    }

    private TimeOnly TimePart
    {
        get => new(BindProperty.Hour, BindProperty.Minute, BindProperty.Second);
        set => BindProperty = new DateTime(BindProperty.Year, BindProperty.Month, BindProperty.Day, value.Hour, value.Minute, value.Second);
    }
}