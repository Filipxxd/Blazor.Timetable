using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using Timetable.Common.Enums;
using Timetable.Common.Utilities;
using Timetable.Configuration;
using Timetable.Services.DataExchange.Export;
using Timetable.Services.Display;
using Timetable.Structure;

namespace Timetable.Components;

public partial class Timetable<TEvent> : IAsyncDisposable where TEvent : class
{
    [Inject] internal IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal WeeklyService WeeklyService { get; set; } = default!;

    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = [];
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    [Parameter] public Expression<Func<TEvent, object?>>? GroupIdentifier { get; set; }
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;

    #region State Change
    [Parameter] public EventCallback OnPreviousClicked { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnTitleClicked { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback<TEvent> OnEventUpdated { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventCreated { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventDeleted { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventsChanged { get; set; } = default!;
    [Parameter] public EventCallback<DayOfWeek> OnChangedToDay { get; set; } = default!;
    #endregion

    #region Templates
    [Parameter, EditorRequired] public RenderFragment<TEvent> CreateTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> EditTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DeleteTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DetailTemplate { get; set; } = default!;
    #endregion

    #region Private Fields
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private CompiledProps<TEvent> _eventProps = default!;
    private IJSObjectReference? _jsModule = default!;
    #endregion

    protected override void OnInitialized()
    {
        _objectReference = DotNetObjectReference.Create(this);

        _eventProps = new CompiledProps<TEvent>(DateFrom, DateTo, Title, GroupIdentifier);
        _timetableManager = new TimetableManager<TEvent>() { Props = _eventProps };

        ExportConfig = new ExportConfig<TEvent>
        {
            FileName = "EventExport",
            Transformer = new CsvTransformer(),
            Properties = [
                new NamePropertySelector<TEvent, DateTime>("DateFrom", DateFrom),
                new NamePropertySelector<TEvent, DateTime>("DateTo", DateTo),
                new NamePropertySelector<TEvent, string>("Title", Title)
            ]
        };
    }

    protected override void OnParametersSet()
    {
        TimetableConfig.Validate();
        ExportConfig.Validate();
        _timetableManager.Grid = CreateGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Timetable/interact.min.js");
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Timetable/Components/Timetable.razor.js");
            await _jsModule.InvokeVoidAsync("dragDrop.init", _objectReference);
        }
    }

    [JSInvokable]
    public void MoveEvent(Guid eventId, Guid targetCellId)
    {
        var timetableEvent = _timetableManager.MoveEvent(eventId, targetCellId);
        if (timetableEvent is null)
        {
            return;
        }

        OnEventUpdated.InvokeAsync(timetableEvent).ConfigureAwait(false);
        StateHasChanged();
    }

    private Grid<TEvent> CreateGrid()
    {
        return TimetableConfig.DisplayType switch
        {
            DisplayType.Day => throw new NotImplementedException(),
            DisplayType.Week => WeeklyService.CreateGrid(Events, TimetableConfig, _eventProps),
            DisplayType.Month => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"Implementation for {nameof(DisplayType)}: '{TimetableConfig.DisplayType}' not found."),
        };
    }

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        TimetableConfig.CurrentDate = DateHelper.GetDateForDay(TimetableConfig.CurrentDate, dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
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
