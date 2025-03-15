using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timetable.Configuration;
using Timetable.Enums;

namespace Timetable.Components;

public partial class Options<TEvent> : ComponentBase, IAsyncDisposable where TEvent : class
{
    private IJSObjectReference? _jsModule = default!;
    
    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter] public IList<TEvent> Events { get; set; } = default!;
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = default!;
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
        
        await _jsModule.InvokeVoidAsync("downloadFileFromStream", fileName, transformResult.StreamReference);
        transformResult.StreamReference.Dispose();
    }
    
    private void HandleDisplayTypeChanged(DisplayType displayType)
    {
        TimetableConfig.DisplayType = displayType;
        OnDisplayTypeChanged.InvokeAsync(displayType);
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