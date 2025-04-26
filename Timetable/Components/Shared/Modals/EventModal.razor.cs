using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Models;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public partial class EventModal<TEvent> where TEvent : class
{
    [Inject] internal ModalService ModalService { get; set; } = default!;

    private RepeatOption RepeatOption { get; set; } = RepeatOption.Once;
    private DateOnly RepeatUntil { get; set; }
    private int RepeatDays { get; set; } = 1;

    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnSave { get; set; }
    [Parameter] public CompiledProps<TEvent> Props { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;

    public bool IsEdit;
    // groupId -> if isedit and datefrom & to changes -> apply all/future/single

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
        var eventsToCreate = new List<TEvent>();
        var baseStart = Props.GetDateFrom(EventWrapper.Event);
        var baseEnd = Props.GetDateTo(EventWrapper.Event);

        eventsToCreate.Add(EventWrapper.Event);

        if (RepeatOption != RepeatOption.Once)
        {
            var groupId = Guid.NewGuid().ToString();
            Props.SetGroupId(EventWrapper.Event, groupId);

            var i = 1;
            while (true)
            {
                DateTime offsetStart, offsetEnd;
                switch (RepeatOption)
                {
                    case RepeatOption.Daily:
                        offsetStart = baseStart.AddDays(1 * i);
                        offsetEnd = baseEnd.AddDays(1 * i);
                        break;
                    case RepeatOption.Weekly:
                        offsetStart = baseStart.AddDays(7 * i);
                        offsetEnd = baseEnd.AddDays(7 * i);
                        break;
                    case RepeatOption.Monthly:
                        offsetStart = baseStart.AddMonths(i);
                        offsetEnd = baseEnd.AddMonths(i);
                        break;
                    case RepeatOption.Custom:
                        offsetStart = baseStart.AddDays(RepeatDays * i);
                        offsetEnd = baseEnd.AddDays(RepeatDays * i);
                        break;
                    default:
                        offsetStart = baseStart;
                        offsetEnd = baseEnd;
                        break;
                }

                if (offsetStart.ToDateOnly() > RepeatUntil)
                    break;

                TEvent newEvent = Activator.CreateInstance<TEvent>();
                Props.SetTitle(newEvent, Props.GetTitle(EventWrapper.Event));
                Props.SetDateFrom(newEvent, offsetStart);
                Props.SetDateTo(newEvent, offsetEnd);
                Props.SetGroupId(newEvent, groupId);

                eventsToCreate.Add(newEvent);
                i++;
            }
        }

        await OnSave.InvokeAsync(eventsToCreate);
        ModalService.Close();
    }
}