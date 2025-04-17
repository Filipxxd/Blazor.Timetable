using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timetable.Common.Enums;
using Timetable.Configuration;

namespace Timetable.Components;

public partial class Options<TEvent> : IAsyncDisposable where TEvent : class
{
    private IJSObjectReference _jsModule = default!;

    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter] public IList<TEvent> Events { get; set; } = default!;
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = default!;
    [Parameter] public DisplayType CurrentDisplayType { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Timetable/Components/Options.razor.js");
        }
    }

    private async Task Export()
    {
        if (_jsModule is null) return;

        var transformResult = ExportConfig.Transformer.Transform(Events, ExportConfig.Properties);
        var fileName = $"{ExportConfig.FileName}.{transformResult.FileExtension}";

        using var stream = transformResult.StreamReference;

        await _jsModule.InvokeVoidAsync("downloadFileFromStream", fileName, stream);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_jsModule is null) return;

        try
        {
            await _jsModule.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}