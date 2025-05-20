using Blazor.Timetable.Common;
using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Components.Modals;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;

namespace Blazor.Timetable.Components;

public partial class TimetableEvent<TEvent>
{
    private readonly Stopwatch _clickStopwatch = new();

    [Parameter] public CellItem<TEvent> CellItem { get; set; } = default!;
    [Parameter] public string BackgroundColor { get; set; } = default!;
    [Parameter] public SpanDirection Direction { get; set; }
    [Parameter] public int Order { get; set; }
    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;
    [Parameter] public EventCallback<UpdateAction<TEvent>> OnEventUpdated { get; set; } = default!;
    [Parameter] public EventCallback<DeleteAction<TEvent>> OnEventDelete { get; set; } = default!;

    [CascadingParameter] internal ModalService ModalService { get; set; } = default!;

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
            { "OriginalEventDescriptor", CellItem.EventDescriptor },
            { "State", EventModalState.Edit },
            { "OnUpdate", OnEventUpdated },
            { "OnDelete", OnEventDelete },
            { "AdditionalFields", AdditionalFields }
        };

        ModalService.Show<EventModal<TEvent>>(parameters);
    }
}