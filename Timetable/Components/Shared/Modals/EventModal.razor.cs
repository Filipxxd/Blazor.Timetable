using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Models.Grid;
using Timetable.Models.Props;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public partial class EventModal<TEvent> where TEvent : class
{
    [Inject] ModalService ModalService { get; set; } = default!;

    [Parameter] public EventModalState State { get; set; }
    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent>? AdditionalFields { get; set; }

    [Parameter] public EventCallback<CreateProps<TEvent>> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnUpdate { get; set; }
    [Parameter] public EventCallback<DeleteProps<TEvent>> OnDelete { get; set; }

    private ActionScope Scope { get; set; } = ActionScope.All;

    private readonly IList<Func<bool>> _validationFuncs = [];
    private void RegisterValidation(Func<bool> fn) => _validationFuncs.Add(fn);
    private EventWrapper<TEvent> _eventWrapper = default!;

    private RepeatOption RepeatOption { get; set; } = RepeatOption.Once;
    private DateTime RepeatUntil { get; set; }
    private int RepeatDays { get; set; } = 1;

    private RepeatOption[] RepetitionOptions => (RepeatOption[])Enum.GetValues(typeof(RepeatOption));
    private ActionScope[] Scopes => (ActionScope[])Enum.GetValues(typeof(ActionScope));

    protected override void OnParametersSet()
    {
        if (EventWrapper.GroupId is null)
            Scope = ActionScope.Single;

        RepeatUntil = EventWrapper.DateFrom.AddMonths(1);

        _eventWrapper = State == EventModalState.Create
            ? EventWrapper
            : EventWrapper.Copy();
    }

    private string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required";
        if (title.Length > 40) return "Max 40 chars";
        return null;
    }

    private string? ValidateDateTo(DateTime d)
    {
        if (d <= _eventWrapper.DateFrom) return "End must be after start";
        return null;
    }

    private async Task SaveAsync()
    {
        if (!Validate())
            return;

        if (State == EventModalState.Create)
        {
            var createProps = new CreateProps<TEvent>
            {
                Repetition = RepeatOption,
                RepeatUntil = RepeatUntil.ToDateOnly(),
                RepeatDays = RepeatDays,
                EventWrapper = _eventWrapper
            };
            await OnCreate.InvokeAsync(createProps);
        }
        else
        {
            var p = new UpdateProps<TEvent>
            {
                Original = EventWrapper,
                New = _eventWrapper,
                Scope = Scope
            };
            await OnUpdate.InvokeAsync(p);
        }

        ModalService.Close();
    }

    private bool Validate()
    {
        var valid = true;

        foreach (var validationFunc in _validationFuncs)
        {
            if (!validationFunc())
                valid = false;
        }

        return valid;
    }

    private void SwitchToDelete()
    {
        State = State == EventModalState.DeleteConfirm
            ? EventModalState.Edit
            : EventModalState.DeleteConfirm;
    }

    private async Task DeleteAsync()
    {
        var p = new DeleteProps<TEvent> { EventWrapper = EventWrapper, Scope = Scope };
        await OnDelete.InvokeAsync(p);
        ModalService.Close();
    }
}