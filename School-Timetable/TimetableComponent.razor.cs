using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using School_Timetable.Structure.Entity;
using School_Timetable.Utilities;
using School_Timetable.Configuration;
using School_Timetable.Enums;
using School_Timetable.Services.DisplayTypeServices;

namespace School_Timetable;

public partial class TimetableComponent<TEvent> : IDisposable where TEvent : class
{
    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal IDisplayTypeService DisplayTypeService { get; set; } = default!;

    #region Event Parameters
    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = [];
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> GroupIdentifier { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    #endregion

    #region State Change
    [Parameter, EditorRequired] public bool IsBusy { get; set; }
    [Parameter, EditorRequired] public Action OnPreviousClicked { get; set; } = default!;
    [Parameter, EditorRequired] public Action OnNextClicked { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter, EditorRequired] public Action<TEvent> OnEventChanged { get; set; } = default!;
    #endregion

    #region Setup
    [Parameter] public DateTime CurrentDate { get; set; } = DateTime.Today;
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    #endregion

    #region Templates
    [Parameter, EditorRequired] public RenderFragment<TEvent> CreateTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> EditTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DeleteTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TEvent> DetailTemplate { get; set; } = default!;
    #endregion

    #region Private Fields
    private DotNetObjectReference<TimetableComponent<TEvent>> _objectReference = default!;
    private IList<GridRow<TEvent>> _rows = [];

    private Func<TEvent, DateTime> _getDateFrom = default!;
    private Func<TEvent, DateTime> _getDateTo = default!;
    private Func<TEvent, string> _getTitle = default!;

    private Action<TEvent, DateTime> _setDateFrom = default!;
    private Action<TEvent, DateTime> _setDateTo = default!;
    #endregion

    protected override void OnInitialized()
    {
        _getDateFrom = PropertyHelper.Get(DateFrom);
        _getDateTo = PropertyHelper.Get(DateTo);
        _getTitle = PropertyHelper.Get(Title);
        _setDateFrom = PropertyHelper.Set(DateFrom);
        _setDateTo = PropertyHelper.Set(DateTo);
        _objectReference = DotNetObjectReference.Create(this);
    }

    protected override void OnParametersSet()
    {
        TimetableConfig.Validate();
        
        UpdateTimetable();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("dragDrop.init", _objectReference);
    }
    
    private void UpdateTimetable()
    {
        _rows.Clear();
        
        switch (TimetableConfig.DefaultDisplayType)
        {
            case DisplayType.Day:
                _rows = DisplayTypeService.CreateGrid(Events, TimetableConfig, _getDateFrom, _getDateTo);
                break;
            case DisplayType.Week:
                _rows = DisplayTypeService.CreateGrid(Events, TimetableConfig, _getDateFrom, _getDateTo);
                break;
            case DisplayType.Month:
                _rows = DisplayTypeService.CreateGrid(Events, TimetableConfig, _getDateFrom, _getDateTo);
                break;
            default:
                throw new NotImplementedException();
        }
    }
    
    [JSInvokable]
    public void MoveEvent(Guid eventId, Guid targetCellId)
    {
        var targetCell = FindCellById(targetCellId);
        var gridItem = FindItemById(eventId);
        if (targetCell is null || gridItem is null) return;
        UpdateEventTiming(gridItem, targetCell.CellTime);
        var currentCell = FindCellByItemId(gridItem.Id);
        if (currentCell is null) return;
        MoveGridItem(currentCell, targetCell, gridItem);
        OnEventChanged.Invoke(gridItem.Event);
        StateHasChanged();
    }

    private GridCell<TEvent>? FindCellById(Guid cellId)
    {
        return _rows.SelectMany(row => row.Cells)
                    .SingleOrDefault(cell => cell.Id == cellId);
    }

    private GridItem<TEvent>? FindItemById(Guid itemId)
    {
        return _rows.SelectMany(row => row.Cells)
                    .SelectMany(cell => cell.Events)
                    .SingleOrDefault(item => item.Id == itemId);
    }

    private void UpdateEventTiming(GridItem<TEvent> gridItem, DateTime newStartDate)
    {
        var oldDateFrom = _getDateFrom(gridItem.Event);
        var oldDateTo = _getDateTo(gridItem.Event);
        var duration = oldDateTo - oldDateFrom;
        var newEndDate = newStartDate.Add(duration);
        _setDateFrom(gridItem.Event, newStartDate);
        _setDateTo(gridItem.Event, newEndDate);
    }

    private GridCell<TEvent>? FindCellByItemId(Guid itemId)
    {
        return _rows.SelectMany(row => row.Cells)
                    .FirstOrDefault(cell => cell.Events.Select(x => x.Id).Contains(itemId));
    }

    private static void MoveGridItem(GridCell<TEvent> fromCell, GridCell<TEvent> toCell, GridItem<TEvent> gridItem)
    {
        fromCell.Events.Remove(gridItem);
        toCell.Events.Add(gridItem);
    }

    public void Dispose()
    {
        if (_objectReference != null)
        {
            _objectReference.Dispose();
            _objectReference = null;
        }
    }
}
    