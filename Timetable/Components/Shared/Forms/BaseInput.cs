using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using Timetable.Common.Exceptions;
using Timetable.Common.Helpers;

namespace Timetable.Components.Shared.Forms;

public abstract class BaseInput<TEvent, TType> : ComponentBase where TEvent : class
{
    protected readonly Guid _id = Guid.NewGuid();
    protected Func<TEvent, TType?> _getter = default!;
    protected Action<TEvent, TType?> _setter = default!;

    [Parameter, EditorRequired] public TEvent Model { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, TType>> Selector { get; set; } = default!;
    [Parameter, EditorRequired] public string Label { get; set; } = default!;
    [Parameter] public bool Required { get; set; } = false;
    [Parameter] public EventCallback<TType> ValueChanged { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (Selector is null)
            throw new InvalidSetupException("Selector is not set");

        _getter = PropertyHelper.CreateGetter(Selector!);
        _setter = PropertyHelper.CreateSetter(Selector!);
    }

    protected virtual TType BindProperty
    {
        get
        {
            return _getter(Model) ?? default!;
        }
        set
        {
            if (EqualityComparer<TType>.Default.Equals(_getter(Model), value))
                return;

            _setter(Model, value);

            if (ValueChanged.HasDelegate)
                ValueChanged.InvokeAsync(value);
        }
    }
}