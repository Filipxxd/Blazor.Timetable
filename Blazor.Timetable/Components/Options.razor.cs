using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Components.Modals;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Timetable.Components;

public partial class Options<TEvent> : IAsyncDisposable where TEvent : class
{
    private IJSObjectReference _jsModule = default!;
    private DotNetObjectReference<Options<TEvent>> _dotNetRef = default!;

    private readonly IEnumerable<DisplayType> _displayTypes = [DisplayType.Day, DisplayType.Week, DisplayType.Month];
    private IEnumerable<DisplayType> AvailableDisplayTypes => _displayTypes.Where(x => x != CurrentDisplayType).OrderBy(x => x);

    [Inject] private Localizer Localizer { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter] public IList<TEvent> Events { get; set; } = default!;
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter] public ImportConfig<TEvent> ImportConfig { get; set; } = default!;
    [Parameter] public DisplayType CurrentDisplayType { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback OnCreateClicked { get; set; }
    [Parameter] public EventCallback<ImportAction<TEvent>> OnImport { get; set; }

    [CascadingParameter] internal ModalService ModalService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Blazor.Timetable/Components/Options.razor.js");
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private async Task HandleCreateClickedAsync()
    {
        await OnCreateClicked.InvokeAsync();
    }

    private async Task HandleImportClickedAsync()
    {
        if (_jsModule is null || _dotNetRef is null) return;
        await _jsModule.InvokeVoidAsync("promptFileSelect", _dotNetRef, ImportConfig.MaxFileSizeBytes, ImportConfig.AllowedExtensions);
    }

    [JSInvokable]
    public async Task ReceiveFileBase64Async(string base64)
    {
        var content = Convert.FromBase64String(base64);
        var stream = new MemoryStream(content, writable: false);
        var items = ImportConfig.Transformer.Transform(stream, ImportConfig.Selectors);

        await stream.DisposeAsync();

        var parameters = new Dictionary<string, object>
        {
            { "ImportedEvents", items },
            { "OnSubmit", OnImport }
        };

        ModalService.Show<ImportModal<TEvent>>(parameters);
    }

    private async Task HandleExportClickedAsync()
    {
        if (_jsModule is null) return;

        var transformResult = ExportConfig.Transformer.Transform(Events, ExportConfig.Selectors);
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