using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using Timetable.Common.Enums;
using Timetable.Common.Helpers;
using Timetable.Models;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public sealed class UpdateProps<TEvent> where TEvent : class
{
    // The list of additional field registrations.
    public List<AdditionalFieldInfo<TEvent>> AdditionalFieldInfos { get; set; } = [];
    public ActionScope Scope { get; set; } = ActionScope.Current;
    public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
}

public partial class UpdateEventModal<TEvent> where TEvent : class
{
    public List<AdditionalFieldInfo<TEvent>> AdditionalFieldInfos { get; } = [];

    private EventWrapper<TEvent> editEvent = default!;

    [Inject] internal ModalService ModalService { get; set; } = default!;
    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnSubmit { get; set; }
    public ActionScope Scope { get; set; } = ActionScope.Current;

    protected override void OnParametersSet()
    {
        var eventCopy = Activator.CreateInstance<TEvent>();

        EventWrapper.Props.SetTitle(eventCopy, EventWrapper.Title);
        EventWrapper.Props.SetDateFrom(eventCopy, EventWrapper.DateFrom);
        EventWrapper.Props.SetDateTo(eventCopy, EventWrapper.DateTo);
        EventWrapper.Props.SetGroupId(eventCopy, EventWrapper.GroupIdentifier);

        editEvent = new EventWrapper<TEvent>()
        {
            Props = EventWrapper.Props,
            Event = eventCopy,
            Span = 0,
            Id = EventWrapper.Id
        };

        foreach (var field in AdditionalFieldInfos)
        {
            // Expecting field.Selector to be Expression<Func<TEvent, object?>>
            if (field.Selector is Expression<Func<TEvent, object?>> selectorExpr)
            {
                // Create a getter and setter for the property.
                var getter = PropertyHelper.CreateGetter<TEvent, object>(selectorExpr);
                var setter = PropertyHelper.CreateSetter<TEvent, object>(selectorExpr);
                var originalValue = getter(EventWrapper.Event);
                setter(eventCopy, originalValue);
            }
        }
    }

    private async Task Submit()
    {
        // In this approach AdditionalFieldsHost has been bound to editEvent so additional inputs
        // already updated editEvent.
        var updateProps = new UpdateProps<TEvent>
        {
            AdditionalFieldInfos = AdditionalFieldInfos, // might be useful for further copy actions
            Scope = Scope,
            EventWrapper = new EventWrapper<TEvent>
            {
                Props = EventWrapper.Props,
                Event = editEvent.Event,
                Span = 0,
                Id = EventWrapper.Id
            }
        };
        // TODO
        await OnSubmit.InvokeAsync(updateProps);
        ModalService.Close();
    }
}