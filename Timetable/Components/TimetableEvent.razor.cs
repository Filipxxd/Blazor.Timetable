using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;
using Timetable.Common.Enums;
using Timetable.Services;

namespace Timetable.Components;

public partial class TimetableEvent
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    private const int MousedownThreshold = 150;
    private readonly Stopwatch _clickStopwatch = new();

    [Parameter] public Guid EventId { get; set; }
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string BackgroundColor { get; set; } = default!;
    [Parameter] public int Span { get; set; }
    [Parameter] public bool IsHeaderEvent { get; set; }
    [Parameter] public SpanDirection? Direction { get; set; }
    [Parameter] public int ColumnIndex { get; set; }
    [Parameter] public int RowIndex { get; set; }
    [Parameter] public int Order { get; set; }
    [Parameter] public int Offset { get; set; }
    [Parameter] public RenderFragment DetailTemplate { get; set; } = default!;
    [Parameter] public RenderFragment EditTemplate { get; set; } = default!;

    private string WrapperStyle
    {
        get
        {
            if (Direction.HasValue)
                return Direction.Value == SpanDirection.Horizontal
                    ? $"grid-template-rows: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1} / span {Span}; grid-row: {RowIndex};"
                    : $"grid-template-columns: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1}; grid-row: {RowIndex} / span {Span};";



            return IsHeaderEvent
                ? $"grid-template-rows: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1} / span {Span}; grid-row: 2;"
                : $"grid-template-columns: repeat({Offset}, 1fr); grid-column: {ColumnIndex + 1}; grid-row: {RowIndex} / span {Span};";
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
        ModalService.Show(Title, builder =>
        {
            builder.AddContent(0, DetailTemplate);
        });
    }
}