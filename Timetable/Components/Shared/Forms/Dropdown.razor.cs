using Microsoft.AspNetCore.Components;

namespace Timetable.Components.Shared.Forms;

public partial class Dropdown<TType>
{
    [Parameter, EditorRequired] public TType[] Options { get; set; } = [];
    [Parameter, EditorRequired] public TType Value { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<TType> ValueChanged { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = default!;
    [Parameter] public string? ErrorMessage { get; set; }

    private TType BoundValue
    {
        get => Value;
        set
        {
            if (EqualityComparer<TType>.Default.Equals(Value, value)) return;

            Value = value;
            ValueChanged.InvokeAsync(value);
        }
    }
}