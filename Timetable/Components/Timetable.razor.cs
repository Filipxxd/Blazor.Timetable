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
    [Inject] internal DailyService DailyService { get; set; } = default!;
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
    private static bool _firstRender = false;
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private CompiledProps<TEvent> _eventProps = default!;
    private IJSObjectReference _jsModule = default!;
    #endregion

    protected override void OnInitialized()
    {
        _firstRender = true;
        _objectReference = DotNetObjectReference.Create(this);

        _eventProps = new CompiledProps<TEvent>(DateFrom, DateTo, Title, GroupIdentifier);
        _timetableManager = new TimetableManager<TEvent>()
        {
            Props = _eventProps,
            CurrentDate = DateTime.Now // TODO: add option to provide custom via _firstRender prop
        };

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
        if (_firstRender)
        {
            _timetableManager.DisplayType = TimetableConfig.DefaultDisplayType;
        }

        TimetableConfig.Validate();
        ExportConfig.Validate();
        _timetableManager.Grid = UpdateGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Timetable/interact.min.js");
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Timetable/Components/Timetable.razor.js");
            await _jsModule.InvokeVoidAsync("dragDrop.init", _objectReference);
            _firstRender = false;
        }
    }

    [JSInvokable]
    public async Task MoveEvent(Guid eventId, Guid targetCellId)
    {
        var timetableEvent = _timetableManager.MoveEvent(eventId, targetCellId);
        if (timetableEvent is null)
        {
            return;
        }

        await OnEventUpdated.InvokeAsync(timetableEvent);
        StateHasChanged();
    }

    private async Task HandleNextClicked()
    {
        // todo: do in manager, calculate current date based on config.days  & months etc.
        _timetableManager.CurrentDate = _timetableManager.DisplayType switch
        {
            DisplayType.Day => _timetableManager.CurrentDate.AddDays(1),
            DisplayType.Week => _timetableManager.CurrentDate.AddDays(7),
            _ => throw new NotImplementedException(),
        };
        StateHasChanged();
        await OnNextClicked.InvokeAsync();
    }

    private async Task HandlePreviousClicked()
    {
        _timetableManager.CurrentDate = _timetableManager.DisplayType switch
        {
            DisplayType.Day => _timetableManager.CurrentDate.AddDays(-1),
            DisplayType.Week => _timetableManager.CurrentDate.AddDays(-7),
            _ => throw new NotImplementedException(),
        };
        StateHasChanged();
        await OnNextClicked.InvokeAsync();
    }

    private async Task HandleDisplayTypeChanged(DisplayType displayType)
    {
        _timetableManager.DisplayType = displayType;
        StateHasChanged();
        await OnNextClicked.InvokeAsync();
    }

    private Grid<TEvent> UpdateGrid()
    {
        return _timetableManager.DisplayType switch
        {
            DisplayType.Day => DailyService.CreateGrid(Events, TimetableConfig, _timetableManager.CurrentDate, _eventProps),
            DisplayType.Week => WeeklyService.CreateGrid(Events, TimetableConfig, _timetableManager.CurrentDate, _eventProps),
            DisplayType.Month => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"Implementation for {nameof(DisplayType)}: '{_timetableManager.DisplayType}' not found."),
        };
    }

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        _timetableManager.CurrentDate = DateHelper.GetDateForDay(_timetableManager.CurrentDate, dayOfWeek);
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
