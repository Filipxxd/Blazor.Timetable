using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Common.Helpers;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Models.Props;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Shared.Modals;

public partial class EventModal<TEvent> where TEvent : class
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    private readonly IList<Func<bool>> _validationFuncs = [];
    private readonly Repeatability[] RepetitionOptions = (Repeatability[])Enum.GetValues(typeof(Repeatability));
    private readonly ActionScope[] Scopes = (ActionScope[])Enum.GetValues(typeof(ActionScope));

    [Parameter] public EventModalState State { get; set; }
    [Parameter] public EventDescriptor<TEvent> OriginalEventDescriptor { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent>? AdditionalFields { get; set; }

    [Parameter] public EventCallback<CreateProps<TEvent>> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnUpdate { get; set; }
    [Parameter] public EventCallback<DeleteProps<TEvent>> OnDelete { get; set; }

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
            : OriginalEventDescriptor.Copy();
    }

    private void RegisterValidation(Func<bool> fn)
        => _validationFuncs.Add(fn);

    private async Task SaveAsync()
    {
        if (!Validate())
            return;

        if (State == EventModalState.Create)
        {
            var createProps = new CreateProps<TEvent>
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
            var updateProps = new UpdateProps<TEvent>
            {
                Original = OriginalEventDescriptor,
                New = EventDescriptor,
                Scope = SelectedScope
            };
            await OnUpdate.InvokeAsync(updateProps);
        }

        ModalService.Close();
    }


    private void ToggleDelete()
    {
        State = State == EventModalState.DeleteConfirm
            ? EventModalState.Edit
            : EventModalState.DeleteConfirm;
    }

    private async Task DeleteAsync()
    {
        var p = new DeleteProps<TEvent> { EventDescriptor = OriginalEventDescriptor, Scope = SelectedScope };
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

    private static string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Required";

        if (title.Length > 128) return "Max length 128";

        return null;
    }

    private string? ValidateDateFrom(DateTime dateTimeFrom)
    {
        if (dateTimeFrom.TimeOfDay.ToTimeOnly() < Config.TimeFrom)
            return $"Must start after {DateTimeHelper.FormatHour(Config.TimeFrom.Hour, Config.Is24HourFormat)}";

        return ValidateDate(dateTimeFrom);
    }

    private string? ValidateDateTo(DateTime dateTimeTo)
    {
        if (dateTimeTo <= EventDescriptor.DateFrom)
            return "Must be after start";

        if (dateTimeTo.TimeOfDay.ToTimeOnly() > Config.TimeTo)
            return $"Must end by {DateTimeHelper.FormatHour(Config.TimeTo.Hour, Config.Is24HourFormat)}";

        return ValidateDate(dateTimeTo);
    }

    private string? ValidateRepeatUntilDate(DateTime repeatUntilDate)
    {
        if (repeatUntilDate <= EventDescriptor.DateTo)
            return "Must be after event end";

        return null;
    }

    private string? ValidateDate(DateTime dateTime)
    {
        if (!Config.Months.Contains((Month)dateTime.Month))
            return $"Invalid month: {dateTime.Month}";

        if (!Config.Days.Contains(dateTime.DayOfWeek))
            return $"Invalid day: {dateTime.DayOfWeek}";

        return null;
    }
}