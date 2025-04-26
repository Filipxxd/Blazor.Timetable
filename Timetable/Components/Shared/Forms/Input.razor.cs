using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using Timetable.Common.Exceptions;
using Timetable.Common.Helpers;

namespace Timetable.Components.Shared.Forms;

public partial class Input<TEvent, TType>
{
    private Func<TEvent, TType?> _getter = default!;
    private Action<TEvent, TType?> _setter = default!;

    [Parameter, EditorRequired] public TEvent Model { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, TType>> Selector { get; set; } = default!;
    [Parameter, EditorRequired] public string Label { get; set; } = default!;
    [Parameter] public EventCallback<TType> ValueChanged { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (Selector is null)
            throw new InvalidSetupException("Selector is not set");

        _getter = PropertyHelper.CreateGetter(Selector!);
        _setter = PropertyHelper.CreateSetter(Selector!);
    }


    private TType BindProperty
    {
        get
        {
            return _getter(Model) ?? default!;
        }
        set
        {
            try
            {
                _setter(Model, value);

                if (ValueChanged.HasDelegate)
                    ValueChanged.InvokeAsync(value);
            }
            catch (Exception ex)
            {
            }
        }
    }

    private static string GetInputType()
    {
        var numberTypes = new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) };
        if (Array.Exists(numberTypes, type => type == typeof(TType)))
            return "number";

        return typeof(TType) switch
        {
            Type type when type == typeof(string) => "text",
            Type type when type == typeof(bool) => "checkbox",
            _ => "text"
        };
    }
}