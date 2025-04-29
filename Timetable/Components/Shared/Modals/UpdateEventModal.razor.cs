using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Models;
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
    public ActionScope Scope { get; set; } = ActionScope.All;

    protected override void OnParametersSet()
    {
        if (EventWrapper.GroupIdentifier is null)
            Scope = ActionScope.Current;

        editEvent = EventWrapper.Copy();
    }

    private async Task Submit()
    {
        var updateProps = new UpdateProps<TEvent>
        {
            Scope = Scope,
            Original = EventWrapper,
            New = new EventWrapper<TEvent>
            {
                Props = EventWrapper.Props,
                Event = editEvent.Event,
                Span = 0,
                Id = EventWrapper.Id
            }
        };

        await OnSubmit.InvokeAsync(updateProps);
        ModalService.Close();
    }
}