using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
using Timetable.Common.Enums;
using Timetable.Components.Shared.Modals;
using Timetable.Models;
using Timetable.Services;

namespace Timetable.Components;

public partial class TimetableEvent<TEvent>
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    private const int MousedownThreshold = 150;
    private readonly Stopwatch _clickStopwatch = new();

    [Parameter] public EventWrapper<TEvent> EventWrapper { get; set; } = default!;
    [Parameter] public string BackgroundColor { get; set; } = default!;
    [Parameter] public bool IsHeaderEvent { get; set; }
    [Parameter] public SpanDirection? Direction { get; set; }
    [Parameter] public int ColumnIndex { get; set; }
    [Parameter] public int RowIndex { get; set; }
    [Parameter] public int Order { get; set; }
    [Parameter] public int Offset { get; set; }
    [Parameter] public RenderFragment DetailTemplate { get; set; } = default!;
    [Parameter] public RenderFragment<TEvent> AdditionalProps { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventUpdated { get; set; } = default!;

    private string WrapperStyle
    {
        get
        {
            if (Direction.HasValue)
                return Direction.Value == SpanDirection.Horizontal
                    ? $"grid-template-rows: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1} / span {EventWrapper.Span}; grid-row: {RowIndex};"
                    : $"grid-template-columns: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1}; grid-row: {RowIndex} / span {EventWrapper.Span};";



            return IsHeaderEvent
                ? $"grid-template-rows: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1} / span {EventWrapper.Span}; grid-row: 2;"
                : $"grid-template-columns: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1}; grid-row: {RowIndex} / span {EventWrapper.Span};";
        }
    }

    private string EventStyle
    {
        get
        {
            string gridSetup;
            if (Direction.HasValue)
                gridSetup = Direction.Value == SpanDirection.Horizontal
                    ? $"grid-row-start: {Order + 1};"
                    : $"grid-column-start: {Order + 1};";
            else
                gridSetup = IsHeaderEvent
                    ? $"grid-row-start: {Order + 1};"
                    : $"grid-column-start: {Order + 1};";

            return $"background-color: {BackgroundColor}; {gridSetup}";
        }
    }

    private void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == 0)
        {
            _clickStopwatch.Restart();
        }
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != 0 || !_clickStopwatch.IsRunning) return;
        _clickStopwatch.Stop();

        if (_clickStopwatch.ElapsedMilliseconds < MousedownThreshold)
        {
            TogglePopover();
        }
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