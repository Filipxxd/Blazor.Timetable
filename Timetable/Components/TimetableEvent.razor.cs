using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Components.Shared.Modals;
using Timetable.Models;
using Timetable.Services;

namespace Timetable.Components;

public partial class TimetableEvent<TEvent>
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    private readonly Stopwatch _clickStopwatch = new();

    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public string BackgroundColor { get; set; } = default!;
    [Parameter] public SpanDirection Direction { get; set; }
    [Parameter] public int Order { get; set; }
    [Parameter] public RenderFragment<TEvent> AdditionalProps { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventUpdated { get; set; } = default!;

    private string EventStyle =>
        $"background-color: {BackgroundColor}; " +
        $"{(Direction == SpanDirection.Horizontal ? $"grid-row-start: {Order + 1};" : $"grid-column-start: {Order + 1};")}";

    private void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != 0) return;

        _clickStopwatch.Restart();
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != 0 || !_clickStopwatch.IsRunning) return;
        _clickStopwatch.Stop();

        if (_clickStopwatch.ElapsedMilliseconds < TimetableConstants.MousedownThreshold)
            TogglePopover();
    }

    private void TogglePopover()
    {
        var onSaveCallback = EventCallback.Factory.Create(this, async (IList<TEvent> events) =>
        {
            await OnEventUpdated.InvokeAsync(events[0]);
        });

        var parameters = new Dictionary<string, object>
        {
            { "EventWrapper", EventWrapper },
            { "OnSave", onSaveCallback },
            { "IsEdit", true },
            { "AdditionalFields", AdditionalProps }
        };

        ModalService.Show<EventModal<TEvent>>("Edit", parameters);
    }
}