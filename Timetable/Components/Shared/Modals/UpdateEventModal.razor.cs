using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Models.Grid;
using Timetable.Models.Props;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public partial class UpdateEventModal<TEvent> where TEvent : class
{
    private EventWrapper<TEvent> editEvent = default!;

    [Inject] internal ModalService ModalService { get; set; } = default!;
    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnSubmit { get; set; }
    [Parameter] public EventCallback<DeleteProps<TEvent>> OnDelete { get; set; }
    public ActionScope Scope { get; set; } = ActionScope.All;
    public UpdateState State { get; set; } = UpdateState.Normal;

    protected override void OnParametersSet()
    {
        if (EventWrapper.GroupId is null)
            Scope = ActionScope.Current;

        editEvent = EventWrapper.Copy();
    }

    private async Task TryDelete()
    {
        if (EventWrapper.HasGroupdAssigned)
        {
            State = UpdateState.Confirm;
            return;
        }

        await Delete();
    }

    private async Task Delete()
    {
        var deleteProps = new DeleteProps<TEvent>
        {
            Scope = Scope,
            EventWrapper = EventWrapper
        };

        await OnDelete.InvokeAsync(deleteProps);
        ModalService.Close();
    }

    private async Task Submit()
    {
        var valid = validationFuncs.All(validate => validate());
        if (!valid)
            return;

        var updateProps = new UpdateProps<TEvent>
        {
            Scope = Scope,
            Original = EventWrapper,
            New = editEvent
        };

        await OnSubmit.InvokeAsync(updateProps);
        ModalService.Close();
    }
    private readonly List<bool> validationStates = [];

    private string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";
        if (title.Length > 40)
            return "Title must be less than 40 characters.";
        return null;
    }

    private string? ValidateDateTo(DateTime dateTo)
    {
        if (dateTo <= editEvent.DateFrom)
            return "End date must be later than the start date.";
        return null;
    }

    public int RegisterValidation(bool initialState = true)
    {
        validationStates.Add(initialState);
        return validationStates.Count - 1;
    }

    public void UpdateValidationState(int index, bool isValid)
    {
        if (index < validationStates.Count)
        {
            validationStates[index] = isValid;
        }
    }

    private bool AreAllInputsValid => validationStates.All(state => state);

    private readonly List<Func<bool>> validationFuncs = [];

    public void RegisterValidation(Func<bool> validateFunc)
    {
        validationFuncs.Add(validateFunc);
    }
}