using Microsoft.AspNetCore.Components;

namespace Timetable.Components.Shared.Forms;

public partial class Input<TType>
{
    [Parameter, EditorRequired] public string Label { get; set; } = default!;
    [Parameter] public bool Required { get; set; } = false;
    [Parameter, EditorRequired] public TType Value { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<TType> ValueChanged { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }

    private string InputType => GetInputType();
    private Guid Id { get; set; } = Guid.NewGuid();

    private string FormattedValue
    {
        get
        {
            if (typeof(TType) == typeof(DateTime) && Value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm");
            }
            return Value?.ToString() ?? string.Empty;
        }
        set
        {
            if (typeof(TType) == typeof(DateTime) && DateTime.TryParse(value, out DateTime dateTime))
            {
                Value = (TType)(object)dateTime;
                ValueChanged.InvokeAsync(Value);
            }
            else if (Value?.ToString() != value)
            {
                Value = (TType)(object)value;
                ValueChanged.InvokeAsync(Value);
            }
        }
    }

    private string GetInputType()
    {
        var numberTypes = new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) };
        if (Array.Exists(numberTypes, type => type == typeof(TType)))
        {
            return "number";
        }
        return typeof(TType) switch
        {
            Type type when type == typeof(string) => "text",
            Type type when type == typeof(bool) => "checkbox",
            Type type when type == typeof(DateTime) => "datetime-local",
            _ => "text"
        };
    }
}