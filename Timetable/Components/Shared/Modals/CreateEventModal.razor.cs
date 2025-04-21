using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Services;
using Timetable.Models;

namespace Timetable.Components.Shared.Modals;

public partial class CreateEventModal<TEvent> where TEvent : class
{
    [Inject] internal ModalService ModalService { get; set; } = default!;

    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnSave { get; set; }
    [Parameter] public CompiledProps<TEvent> Props { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent> CreateFields { get; set; } = default!;

    private RepeatOption SelectedRepeatOption { get; set; } = RepeatOption.Once;
    private int CustomEveryDays { get; set; } = 1;

    private async Task Save()
    {
        var eventsToCreate = new List<TEvent>();
        var baseStart = Props.GetDateFrom(EventWrapper.Event);
        var baseEnd = Props.GetDateTo(EventWrapper.Event);

        eventsToCreate.Add(EventWrapper.Event);

        var endThreshold = baseStart.AddYears(10); // TODO: Threshold in config

        if (SelectedRepeatOption != RepeatOption.Once)
        {
            var groupId = Guid.NewGuid().ToString();
            Props.SetGroupId(EventWrapper.Event, groupId);

            var i = 1;
            while (true)
            {
                DateTime offsetStart, offsetEnd;
                switch (SelectedRepeatOption)
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
                        offsetStart = baseStart.AddDays(CustomEveryDays * i);
                        offsetEnd = baseEnd.AddDays(CustomEveryDays * i);
                        break;
                    default:
                        offsetStart = baseStart;
                        offsetEnd = baseEnd;
                        break;
                }

                if (offsetStart > endThreshold)
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