using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timetable.Structure.Entity;
using Timetable.Utilities;
using Timetable.Configuration;
using Timetable.Enums;
using Timetable.Services.Display;

namespace Timetable;

public partial class TimetableComponent<TEvent> : IDisposable where TEvent : class
{
    [Inject] internal IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;

    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = [];
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    [Parameter] public Expression<Func<TEvent, object?>> GroupIdentifier { get; set; } = default!;

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
    private IList<GridRow<TEvent>> _rows = [];

    private Func<TEvent, DateTime> _getDateFrom = default!;
    private Func<TEvent, DateTime> _getDateTo = default!;
    private Func<TEvent, string?> _getTitle = default!;
    private Func<TEvent, object?> _getGroupIdentifier  = default!;

    private Action<TEvent, DateTime> _setDateFrom = default!;
    private Action<TEvent, DateTime> _setDateTo = default!;
    private Action<TEvent, object?> _setGroupIdentifier = default!;
    #endregion

    protected override void OnInitialized()
    {
        _getDateFrom = PropertyHelper.CreateGetter(DateFrom);
        _getDateTo = PropertyHelper.CreateGetter(DateTo);
        _getTitle = PropertyHelper.CreateGetter(Title);
        _getGroupIdentifier = PropertyHelper.CreateGetter(GroupIdentifier);
        _setDateFrom = PropertyHelper.CreateSetter(DateFrom);
        _setDateTo = PropertyHelper.CreateSetter(DateTo);
        _setGroupIdentifier = PropertyHelper.CreateSetter(GroupIdentifier);
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
        _rows = DisplayServices.FirstOrDefault(x => x.DisplayType == TimetableConfig.DisplayType)
                    ?.CreateGrid(Events, TimetableConfig, _getDateFrom, _getDateTo)
                ?? throw new NotSupportedException($"Implementation of {nameof(IDisplayService)} for {nameof(DisplayType)} '{TimetableConfig.DisplayType.ToString()}' not found.");
    }
    
    [JSInvokable]
    public void MoveEvent(Guid eventId, Guid targetCellId)
    {
        // TODO: Add group event logic (modal to confirm either group update or single update)
        
        var targetCell = FindCellById(targetCellId);
        var gridItem = FindItemById(eventId);
        if (targetCell is null || gridItem is null) return;
        UpdateEventTiming(gridItem, targetCell.CellTime);
        var currentCell = FindCellByItemId(gridItem.Id);
        if (currentCell is null) return;
        MoveGridItem(currentCell, targetCell, gridItem);
        OnEventUpdated.Invoke(gridItem.Event);
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

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        OnChangedToDay.Invoke(dayOfWeek);
        TimetableConfig.CurrentDate = DateHelper.GetDateForDay(TimetableConfig.CurrentDate, dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _objectReference.Dispose();

        _disposed = true;
    }
}
    