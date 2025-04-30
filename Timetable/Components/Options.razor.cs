using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timetable.Common.Enums;
using Timetable.Models.Configuration;

namespace Timetable.Components;

public partial class Options<TEvent> : IAsyncDisposable where TEvent : class
{
    private IJSObjectReference _jsModule = default!;
    private DotNetObjectReference<Options<TEvent>> _dotNetRef = default!;

    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter] public IList<TEvent> Events { get; set; } = default!;
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter] public ImportConfig<TEvent> ImportConfig { get; set; } = default!;
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = default!;
    [Parameter] public DisplayType CurrentDisplayType { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback OnCreateClicked { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Timetable/Components/Options.razor.js");
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private async Task HandleCreateClicked()
    {
        await OnCreateClicked.InvokeAsync();
    }

    private async Task Import()
    {
        if (_jsModule is null || _dotNetRef is null) return;
        await _jsModule.InvokeVoidAsync("promptFileSelect",
           _dotNetRef,
           ImportConfig.MaxFileSizeBytes,
           ImportConfig.AllowedExtensions);
    }

    [JSInvokable]
    public async Task ReceiveFileBase64(string base64)
    {
        var content = Convert.FromBase64String(base64);
        var ms = new MemoryStream(content, writable: false);
        var items = ImportConfig.Transformer.Transform(ms);

        await ms.DisposeAsync();
        //await OnImported.InvokeAsync(items);
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
        _dotNetRef?.Dispose();

        if (_jsModule is null) return;

        try
        {
            await _jsModule.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}