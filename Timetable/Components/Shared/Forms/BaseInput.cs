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

    protected string? ErrorMessage { get; private set; }
    protected bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [Parameter, EditorRequired] public TEvent Model { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, TType?>> Selector { get; set; } = default!;
    [Parameter, EditorRequired] public string Label { get; set; } = default!;
    [Parameter] public Func<TType, string?>? Validate { get; set; }
    [Parameter] public bool Required { get; set; } = false;
    [Parameter] public EventCallback<TType> ValueChanged { get; set; } = default!;

    [CascadingParameter(Name = "RegisterValidation")] public Action<Func<bool>>? RegisterValidation { get; set; }

    protected override void OnParametersSet()
    {
        if (Selector is null)
            throw new InvalidSetupException("Selector is not set");

        RegisterValidation?.Invoke(ValidateInput);

        _getter = PropertyHelper.CreateGetter(Selector!);
        _setter = PropertyHelper.CreateSetter(Selector!);
    }

    private bool ValidateInput()
    {
        if (Validate != null)
        {
            ErrorMessage = Validate(_getter(Model)!);
            return string.IsNullOrEmpty(ErrorMessage);
        }
        return true;
    }

    protected virtual TType BindProperty
    {
        get => _getter(Model) ?? default!;
        set
        {
            if (EqualityComparer<TType>.Default.Equals(_getter(Model), value)) return;

            _setter(Model, value);
            ValidateInput();
        }
    }
}