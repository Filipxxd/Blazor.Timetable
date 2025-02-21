using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using School_Timetable.Structure.Entity;
using School_Timetable.Utilities;
using Microsoft.Extensions.Localization;
using School_Timetable.Configuration;
using School_Timetable.Enums;

namespace School_Timetable;

public partial class TimetableComponent<TEvent> : IDisposable where TEvent : class
{
    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] public IStringLocalizer<TimetableComponent<TEvent>> Localizer { get; set; } = default!;

    #region Event Parameters
    [Parameter, EditorRequired] public IEnumerable<TEvent> Events { get; set; } = [];
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
    private readonly List<GridRow<TEvent>> _rows = [];
    private Dictionary<(DateTime Date, int Hour), List<TEvent>> _eventCache = new();

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

        _eventCache = Events.GroupBy(e => (Date: _getDateFrom(e).Date, Hour: _getDateFrom(e).Hour))
                            .ToDictionary(g => g.Key, g => g.ToList());
        InitializeTimetable();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("dragDrop.init", _objectReference);
    }
    
    private void InitializeTimetable()
    {
        _rows.Clear();
        var hours = Enumerable.Range(TimetableConfig.HourFrom, TimetableConfig.HourTo - TimetableConfig.HourFrom + 1);

        switch (TimetableConfig.DefaultDisplayType)
        {
            case DisplayType.Day:
                InitializeDailyView(hours);
                break;
            case DisplayType.Week:
                InitializeWeeklyView(hours);
                break;
            case DisplayType.Month:
                InitializeMonthlyView(hours);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void InitializeDailyView(IEnumerable<int> hours)
    {
        foreach (var hour in hours)
        {
            var currentDay = CurrentDate.Date;
            var rowStartTime = currentDay.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
            var cellDate = currentDay.Date;
            _eventCache.TryGetValue((cellDate, hour), out var eventsAtSlot);
            var items = eventsAtSlot?.Select(e => new GridItem<TEvent>
            {
                Id = Guid.NewGuid(),
                EventDetail = e
            }).ToList() ?? [];

            var gridCell = new GridCell<TEvent>
            {
                Id = Guid.NewGuid(),
                CellTime = cellDate.AddHours(hour),
                Events = items
            };
            gridRow.Cells.Add(gridCell);
            _rows.Add(gridRow);
        }
    }
    
    private void InitializeWeeklyView(IEnumerable<int> hours)
    {
        _rows.Clear();
        var startOfWeek = DateHelper.GetStartOfWeekDate(CurrentDate, TimetableConfig.SupportedDays.First());

        foreach (var hour in hours)
        {
            var rowStartTime = startOfWeek.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };

            foreach (var dayOffset in Enumerable.Range(0, 7))
            {
                var cellDate = startOfWeek.AddDays(dayOffset).Date;

                _eventCache.TryGetValue((cellDate, hour), out var eventsAtSlot);
                
                var items = eventsAtSlot?.Select(e => 
                {
                    var eventStart = _getDateFrom(e);
                    var eventEnd = _getDateTo(e);
                    
                    if (eventEnd.Hour >= TimetableConfig.HourTo && eventStart.Hour <= TimetableConfig.HourFrom)
                        return null;
                    
                    var span = (int)(eventEnd - eventStart).TotalHours;

                    return new GridItem<TEvent>
                    {
                        Id = Guid.NewGuid(),
                        EventDetail = e,
                        IsWholeDay = eventEnd.Hour >= TimetableConfig.HourTo && eventStart.Hour <= TimetableConfig.HourFrom,
                        Span = span
                    };
                }).ToList() ?? [];

                var gridCell = new GridCell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    CellTime = cellDate.AddHours(hour),
                    Events = items
                };

                gridRow.Cells.Add(gridCell);
            }

            _rows.Add(gridRow);
        }
    }

    private void InitializeMonthlyView(IEnumerable<int> hours)
    {
        var startOfMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);

        foreach (var dayOfMonth in Enumerable.Range(0, daysInMonth))
        {
            var currentDay = startOfMonth.AddDays(dayOfMonth);
            foreach (var hour in hours)
            {
                var rowStartTime = currentDay.AddHours(hour);
                var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
                var cellDate = currentDay.Date;
                _eventCache.TryGetValue((cellDate, hour), out var eventsAtSlot);
                var items = eventsAtSlot?.Select(e => new GridItem<TEvent>
                {
                    Id = Guid.NewGuid(),
                    EventDetail = e
                }).ToList() ?? [];

                var gridCell = new GridCell<TEvent>
                {
                    Id = Guid.NewGuid(),
                    CellTime = cellDate.AddHours(hour),
                    Events = items
                };
                gridRow.Cells.Add(gridCell);
                _rows.Add(gridRow);
            }
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
        OnEventChanged.Invoke(gridItem.EventDetail);
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
        var oldDateFrom = _getDateFrom(gridItem.EventDetail);
        var oldDateTo = _getDateTo(gridItem.EventDetail);
        var duration = oldDateTo - oldDateFrom;
        var newEndDate = newStartDate.Add(duration);
        _setDateFrom(gridItem.EventDetail, newStartDate);
        _setDateTo(gridItem.EventDetail, newEndDate);
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
    