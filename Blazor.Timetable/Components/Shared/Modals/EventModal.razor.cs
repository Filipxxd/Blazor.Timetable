using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Models.Props;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Shared.Modals;

public partial class EventModal<TEvent> where TEvent : class
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    [Parameter] public EventModalState State { get; set; }
    [Parameter] public EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent>? AdditionalFields { get; set; }

    [Parameter] public EventCallback<CreateProps<TEvent>> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnUpdate { get; set; }
    [Parameter] public EventCallback<DeleteProps<TEvent>> OnDelete { get; set; }

    [CascadingParameter] public TimetableConfig Config { get; set; } = default!;

    private ActionScope Scope { get; set; } = ActionScope.All;

    private readonly IList<Func<bool>> _validationFuncs = [];
    private void RegisterValidation(Func<bool> fn) => _validationFuncs.Add(fn);
    private EventDescriptor<TEvent> _eventDescriptor = default!;

    private RepeatOption RepeatOption { get; set; } = RepeatOption.Once;
    private DateTime RepeatUntil { get; set; }
    private int RepeatDays { get; set; } = 1;

    private RepeatOption[] RepetitionOptions => (RepeatOption[])Enum.GetValues(typeof(RepeatOption));
    private ActionScope[] Scopes => (ActionScope[])Enum.GetValues(typeof(ActionScope));

    protected override void OnParametersSet()
    {
        if (EventDescriptor.GroupId is null)
            Scope = ActionScope.Single;

        RepeatUntil = EventDescriptor.DateFrom.AddMonths(1);

        _eventDescriptor = State == EventModalState.Create
            ? EventDescriptor
            : EventDescriptor.Copy();
    }

    private string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required";
        if (title.Length > 40) return "Max 40 chars";
        return null;
    }

    private string? ValidateDateTo(DateTime d)
    {
        if (d <= _eventDescriptor.DateFrom) return "End must be after start";

        if (Config.TimeTo < _eventDescriptor.DateTo.TimeOfDay.ToTimeOnly())
            return "something is wrong i can feel it";

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
                EventDescriptor = _eventDescriptor
            };
            await OnCreate.InvokeAsync(createProps);
        }
        else
        {
            var p = new UpdateProps<TEvent>
            {
                Original = EventDescriptor,
                New = _eventDescriptor,
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

    private void ToggleDelete()
    {
        State = State == EventModalState.DeleteConfirm
            ? EventModalState.Edit
            : EventModalState.DeleteConfirm;
    }

    private async Task DeleteAsync()
    {
        var p = new DeleteProps<TEvent> { EventDescriptor = EventDescriptor, Scope = Scope };
        await OnDelete.InvokeAsync(p);
        ModalService.Close();
    }
}