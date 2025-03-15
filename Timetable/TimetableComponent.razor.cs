using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timetable.Structure;
using Timetable.Utilities;
using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Services.DataExchange.Export;
using Timetable.Services.Display;

namespace Timetable;

public partial class TimetableComponent<TEvent> : IAsyncDisposable where TEvent : class
{
    [Inject] internal IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;

    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = [];
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string?>> Title { get; set; } = default!;
    [Parameter] public Expression<Func<TEvent, object?>>? GroupIdentifier { get; set; }

    #region State Change
    [Parameter] public Action OnPreviousClicked { get; set; } = default!;
    [Parameter] public Action OnNextClicked { get; set; } = default!;
    [Parameter] public Action<TEvent> OnTitleClicked { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public Action<TEvent> OnEventUpdated { get; set; } = default!;
    [Parameter] public Action<TEvent> OnEventCreated { get; set; } = default!;
    [Parameter] public Action<TEvent> OnEventDeleted { get; set; } = default!;
    [Parameter] public Action<IList<TEvent>> OnGroupEventsChanged { get; set; } = default!;
    [Parameter] public Action<DayOfWeek> OnChangedToDay { get; set; } = default!;
    
    #endregion

    #region Templates
    [Parameter, EditorRequired] public RenderFragment<TEvent> CreateTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> EditTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DeleteTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DetailTemplate { get; set; } = default!;
    #endregion

    #region Private Fields
    private bool _disposed = false;
    private DotNetObjectReference<TimetableComponent<TEvent>> _objectReference = default!;
    private Timetable<TEvent> _timetable = default!;
    private TimetableEventProps<TEvent> _eventProps = default!;
    private IJSObjectReference? _jsModule = default!;
    #endregion

    protected override void OnInitialized()
    {
        _objectReference = DotNetObjectReference.Create(this);
        
        _timetable = new Timetable<TEvent>();
        _eventProps = new TimetableEventProps<TEvent>(DateFrom, DateTo, Title, GroupIdentifier);

        ExportConfig = new ExportConfig<TEvent>
        {
            FileName = "EventExport",
            Transformer = new CsvTransformer(),
            Properties = [
                new NamePropertySelector<TEvent, DateTime>("DateFrom", DateFrom), 
                new NamePropertySelector<TEvent, DateTime>("DateTo", DateTo), 
                new NamePropertySelector<TEvent, string?>("Title", Title)
            ]
        };
    }

    protected override void OnParametersSet()
    {
        TimetableConfig.Validate();
        ExportConfig.Validate();
        _timetable.Rows = DisplayServices.FirstOrDefault(x => x.DisplayType == TimetableConfig.DisplayType)
                              ?.CreateGrid(Events, TimetableConfig, _eventProps)
                          ?? throw new NotSupportedException($"Implementation of {nameof(IDisplayService)} for {nameof(DisplayType)} '{TimetableConfig.DisplayType.ToString()}' not found.");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Timetable/interact.min.js");
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/Timetable/TimetableComponent.razor.js");
            await _jsModule.InvokeVoidAsync("dragDrop.init", _objectReference);
        }
    }
    
    [JSInvokable]
    public void MoveEvent(Guid eventId, Guid targetCellId)
    {
        if (!_timetable.TryMoveEvent(eventId, targetCellId, out var @event))
        {
            return; 
        }
        
        OnEventUpdated.Invoke(@event);
        StateHasChanged();
    }

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        OnChangedToDay.Invoke(dayOfWeek);
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
    