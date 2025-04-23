using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;

namespace Timetable.Components.Shared;

public partial class GridItem
{
    [Parameter, EditorRequired] public int RowIndex { get; set; }
    [Parameter, EditorRequired] public int ColumnIndex { get; set; }
    [Parameter] public Guid? SlotId { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public int? Span { get; set; }
    [Parameter] public SpanDirection Direction { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }

    private string GetGridStyle()
    {
        var rowStyle = $"grid-row: {RowIndex}";
        var columnStyle = $"grid-column: {ColumnIndex}";

        if (Span.HasValue)
        {
            switch (Direction)
            {
                case SpanDirection.Horizontal:
                    rowStyle += $" / span {Span.Value}";
                    break;
                case SpanDirection.Vertical:
                    columnStyle += $" / span {Span.Value}";
                    break;
            }
        }

        return $"{columnStyle}; {rowStyle};";
    }
}