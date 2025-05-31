using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Modals;

public partial class EventModal<TEvent> where TEvent : class
{
    [Inject] private Localizer Localizer { get; set; } = default!;

    private readonly IList<Func<bool>> _validationFuncs = [];
    private readonly Repeatability[] RepetitionOptions = (Repeatability[])Enum.GetValues(typeof(Repeatability));
    private readonly ActionScope[] Scopes = (ActionScope[])Enum.GetValues(typeof(ActionScope));

    [Parameter] public EventModalState State { get; set; }
    [Parameter] public EventDescriptor<TEvent> OriginalEventDescriptor { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent>? AdditionalFields { get; set; }

    [Parameter] public EventCallback<CreateAction<TEvent>> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateAction<TEvent>> OnUpdate { get; set; }
    [Parameter] public EventCallback<DeleteAction<TEvent>> OnDelete { get; set; }

    [CascadingParameter] internal ModalService ModalService { get; set; } = default!;
    [CascadingParameter] public TimetableConfig Config { get; set; } = default!;

    private EventDescriptor<TEvent> EventDescriptor { get; set; } = default!;

    private ActionScope SelectedScope { get; set; } = ActionScope.All;
    private Repeatability SelectedRepeatability { get; set; } = Repeatability.Once;
    private DateTime RepeatUntil { get; set; }
    private int RepeatAfterDays { get; set; } = 1;

    protected override void OnParametersSet()
    {
        if (OriginalEventDescriptor.GroupId is null)
            SelectedScope = ActionScope.Single;

        RepeatUntil = OriginalEventDescriptor.DateFrom.AddMonths(1);

        EventDescriptor = State == EventModalState.Create
            ? OriginalEventDescriptor
            : OriginalEventDescriptor.DeepCopy();
    }

    private void RegisterValidation(Func<bool> fn)
        => _validationFuncs.Add(fn);

    private async Task SaveAsync()
    {
        if (!Validate())
            return;

        if (State == EventModalState.Create)
        {
            var createProps = new CreateAction<TEvent>
            {
                Repetition = SelectedRepeatability,
                RepeatUntil = RepeatUntil.ToDateOnly(),
                RepeatDays = RepeatAfterDays,
                EventDescriptor = EventDescriptor
            };
            await OnCreate.InvokeAsync(createProps);
        }
        else
        {
            var updateProps = new UpdateAction<TEvent>
            {
                Original = OriginalEventDescriptor,
                New = EventDescriptor,
                Scope = SelectedScope
            };
            await OnUpdate.InvokeAsync(updateProps);
        }

        ModalService.Close();
    }


    private async Task ToggleDelete()
    {
        if (SelectedScope == ActionScope.Single)
        {
            await DeleteAsync();
            return;
        }

        State = State == EventModalState.DeleteConfirm
            ? EventModalState.Edit
            : EventModalState.DeleteConfirm;
    }

    private async Task DeleteAsync()
    {
        var p = new DeleteAction<TEvent> { EventDescriptor = OriginalEventDescriptor, Scope = SelectedScope };
        await OnDelete.InvokeAsync(p);
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

    private string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Localizer["ValidationNotEmpty"];

        if (title.Length > 128) return Localizer.GetLocalizedString("ValidationMaxLength", 128);

        return null;
    }

    private string? ValidateDateFrom(DateTime dateTimeFrom)
    {
        return ValidateDate(dateTimeFrom);
    }

    private string? ValidateDateTo(DateTime dateTimeTo)
    {
        if (dateTimeTo <= EventDescriptor.DateFrom)
            return Localizer["ValidationBeginAfterStart"];

        return ValidateDate(dateTimeTo);
    }

    private string? ValidateRepeatUntilDate(DateTime repeatUntilDate)
    {
        if (repeatUntilDate <= EventDescriptor.DateTo)
            return Localizer["ValidationBeAfterEnd"];

        return null;
    }

    private string? ValidateDate(DateTime dateTime)
    {
        if (!Config.Months.Contains((Month)dateTime.Month))
            return Localizer.GetLocalizedString("ValidationInvalidMonth", dateTime.Month);

        if (!Config.Days.Contains(dateTime.DayOfWeek))
            return Localizer.GetLocalizedString("ValidationInvalidDay", dateTime.DayOfWeek);

        return null;
    }
}