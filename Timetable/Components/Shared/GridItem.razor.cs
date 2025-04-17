using Microsoft.AspNetCore.Components;

namespace Timetable.Components.Shared;

public partial class GridItem
{
    [Parameter, EditorRequired] public int RowIndex { get; set; }
    [Parameter, EditorRequired] public int ColumnIndex { get; set; }
    [Parameter] public Guid? SlotId { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}