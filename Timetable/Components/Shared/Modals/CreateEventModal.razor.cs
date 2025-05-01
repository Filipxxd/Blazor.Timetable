using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Models.Grid;
using Timetable.Models.Props;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public partial class CreateEventModal<TEvent> where TEvent : class
{
    [Inject] internal ModalService ModalService { get; set; } = default!;

    private RepeatOption RepeatOption { get; set; } = RepeatOption.Once;
    private DateOnly RepeatUntil { get; set; }
    private int RepeatDays { get; set; } = 1;

    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public EventCallback<CreateProps<TEvent>> OnCreate { get; set; }
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;

    protected override void OnParametersSet()
    {
        RepeatUntil = EventWrapper.DateFrom.AddMonths(1).ToDateOnly();
    }

    private void OnRepeatOptionChanged(RepeatOption opt)
    {
        StateHasChanged();
    }

    private async Task Save()
    {
        var createProps = new CreateProps<TEvent>
        {
            Repetition = RepeatOption,
            RepeatUntil = RepeatUntil,
            RepeatDays = RepeatDays,
            EventWrapper = EventWrapper
        };

        await OnCreate.InvokeAsync(createProps);
        ModalService.Close();
    }
}