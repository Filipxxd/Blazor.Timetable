using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using School_Timetable.Structure.Entity;
using School_Timetable.Utilities;
using Microsoft.Extensions.Localization;
using School_Timetable.Enums;
using School_Timetable.Exceptions;

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
    [Parameter] public bool HourFormat24 { get; set; } = true;
    [Parameter] public DateTime CurrentDate { get; set; } = DateTime.Today;
    [Parameter] public DisplayType DefaultDisplayType { get; set; } = DisplayType.Week;
    [Parameter] public IEnumerable<DisplayType> SupportedDisplayTypes { get; set; } = [DisplayType.Day, DisplayType.Week, DisplayType.Month];
    [Parameter] public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;
    [Parameter] public IEnumerable<DayOfWeek> SupportedDays { get; set; } =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];
    [Parameter] public Month FirstMonthOfYear { get; set; } = Month.January;
    [Parameter] public IEnumerable<Month> SupportedMonths { get; set; } =
    [
       Month.January, Month.February, Month.March, Month.April, Month.May, Month.June, Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];
    [Parameter] public int HourFrom { get; set; } = 8;
    [Parameter] public int HourTo { get; set; } = 23;
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
        if (HourFrom < 0)
            throw new InvalidSetupException($"{nameof(HourFrom)} must be >= 0.");
        if (HourTo > 23)
            throw new InvalidSetupException($"{nameof(HourTo)} must be <= 23.");
        if (HourTo < HourFrom)
            throw new InvalidSetupException($"{nameof(HourTo)} must be greater than or equal to HourFrom.");
        if (!SupportedDays.Contains(FirstDayOfWeek))
            throw new InvalidSetupException($"{nameof(FirstDayOfWeek)} must be in {nameof(SupportedDays)}.");
        if (!SupportedDays.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(SupportedDays)} required.");
        if (!SupportedDisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(DisplayType)} in {nameof(SupportedDisplayTypes)} required.");

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
        var startOfWeek = DateHelper.GetStartOfWeekDate(CurrentDate, FirstDayOfWeek);
        var hours = Enumerable.Range(HourFrom, HourTo - HourFrom + 1);
        foreach (var hour in hours)
        {
            var rowStartTime = startOfWeek.AddHours(hour);
            var gridRow = new GridRow<TEvent> { RowStartTime = rowStartTime };
            foreach (var dayOffset in Enumerable.Range(0, 7))
            {
                var cellDate = startOfWeek.AddDays(dayOffset).Date;
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
            }
            _rows.Add(gridRow);
        }
    }

    public string GetHeaderTitle()
    {
        var displayType = DefaultDisplayType;
        return displayType switch
        {
            DisplayType.Day => CurrentDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture),
            DisplayType.Week => $"{CurrentDate:dd MMMM yyyy} - {CurrentDate.AddDays(6):dd MMMM yyyy}",
            DisplayType.Month => $"{CurrentDate:MMMM yyyy}",
            _ => throw new NotImplementedException()
        };
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
    