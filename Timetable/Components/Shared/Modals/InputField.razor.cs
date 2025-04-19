using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using Timetable.Common.Exceptions;
using Timetable.Common.Helpers;

namespace Timetable.Components.Shared.Modals;

public partial class InputField<TEvent, TProperty> where TEvent : class
{
    [Parameter, EditorRequired] public Expression<Func<TEvent, TProperty>> Selector { get; set; } = default!;
    [Parameter, EditorRequired] public TEvent EventInstance { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TProperty?, bool> Validate { get; set; } = default!;
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    private string? ErrorMessage { get; set; }

    private Func<TEvent, TProperty?> Getter = default!;
    private Action<TEvent, TProperty?> Setter = default!;

    protected override void OnParametersSet()
    {
        if (Selector is null)
            throw new InvalidSetupException("Selector is not set");

        Getter = PropertyHelper.CreateGetter(Selector!);
        Setter = PropertyHelper.CreateSetter(Selector!);
    }

    private TProperty? FieldValue
    {
        get
        {
            return Getter(EventInstance);
        }
        set
        {
            try
            {
                ErrorMessage = Validate != null && !Validate(value)
                    ? "Invalid"
                    : null;

                Setter(EventInstance, value);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Conversion error: {ex.Message}";
            }
        }
    }
}