using Blazor.Timetable.Common.Enums;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Shared;

public partial class GridItem
{
    [Parameter, EditorRequired] public int RowIndex { get; set; }
    [Parameter, EditorRequired] public int ColumnIndex { get; set; }
    [Parameter, EditorRequired] public SpanDirection Direction { get; set; }
    [Parameter] public Guid? SlotId { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public int? Span { get; set; }
    [Parameter] public int Offset { get; set; } = 0;
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string Style => Direction == SpanDirection.Horizontal
        ? $"display: grid; grid-template-rows: repeat({Offset}, 1fr); grid-column: {ColumnIndex} {(Span.HasValue ? $"/ span {Span}" : null)}; grid-row: {RowIndex}"
        : $"display: grid; grid-template-columns: repeat({Offset}, 1fr); grid-column: {ColumnIndex}; grid-row: {RowIndex} {(Span.HasValue ? $"/ span {Span}" : null)};";

    private async Task HandleClick()
        => await OnClick.InvokeAsync();
}