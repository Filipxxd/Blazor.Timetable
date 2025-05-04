using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
using Blazor.Timetable.Common;
using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Components.Shared.Modals;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Models.Props;
using Blazor.Timetable.Services;

namespace Blazor.Timetable.Components;

public partial class TimetableEvent<TEvent>
{
    private readonly Stopwatch _clickStopwatch = new();

    [Inject] private ModalService ModalService { get; set; } = default!;

    [Parameter] public CellItem<TEvent> CellItem { get; set; } = default!;
    [Parameter] public string BackgroundColor { get; set; } = default!;
    [Parameter] public SpanDirection Direction { get; set; }
    [Parameter] public int Order { get; set; }
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;
    [Parameter] public EventCallback<UpdateProps<TEvent>> OnEventUpdated { get; set; } = default!;
    [Parameter] public EventCallback<DeleteProps<TEvent>> OnEventDelete { get; set; } = default!;

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
        var parameters = new Dictionary<string, object>
        {
            { "EventWrapper", CellItem.EventWrapper },
            { "State", EventModalState.Edit },
            { "OnUpdate", OnEventUpdated },
            { "OnDelete", OnEventDelete },
            { "AdditionalFields", AdditionalFields }
        };

        ModalService.Show<EventModal<TEvent>>("Edit", parameters);
    }
}