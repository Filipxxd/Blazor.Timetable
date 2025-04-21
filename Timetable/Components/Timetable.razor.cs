using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Components.Shared.Modals;
using Timetable.Configuration;
using Timetable.Models;
using Timetable.Services;
using Timetable.Services.DataExchange.Export;
using Timetable.Services.Display;

namespace Timetable.Components;

public partial class Timetable<TEvent> : IAsyncDisposable where TEvent : class
{
    [Inject] internal IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;
    [Inject] internal ModalService ModalService { get; set; } = default!;

    [Parameter, EditorRequired] public ObservableCollection<TEvent> Events { get; set; } = [];
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, object?>> GroupIdentifier { get; set; } = default!;
    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;

    #region State Change
    [Parameter] public EventCallback OnPreviousClicked { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnTitleClicked { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback<TEvent> OnEventUpdated { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventCreated { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventCreated { get; set; } = default!;
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
    private bool _firstRender = false;
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
            Props = _eventProps
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
            _timetableManager.CurrentDate = DateTime.Now; // TODO: add option to provide custom via _firstRender prop;

            while (!_timetableManager.CurrentDate.IsValidDateTime(TimetableConfig.Days, TimetableConfig.Months))
                _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetNextValidDate(TimetableConfig.Days, TimetableConfig.Months);
        }

        TimetableConfig.Validate();
        ExportConfig.Validate();

        _timetableManager.Grid = GenerateGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Timetable/interact.min.js");
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Timetable/Components/Timetable.razor.js");
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
        _timetableManager.NextDate(TimetableConfig);
        await OnNextClicked.InvokeAsync();
        _timetableManager.Grid = GenerateGrid();
    }

    private async Task HandlePreviousClicked()
    {
        _timetableManager.PreviousDate(TimetableConfig);
        await OnPreviousClicked.InvokeAsync();
        _timetableManager.Grid = GenerateGrid();
    }

    private async Task HandleDisplayTypeChanged(DisplayType displayType)
    {
        _timetableManager.DisplayType = displayType;
        // TODO: default via param
        _timetableManager.CurrentDate = DateTime.Now;

        await OnNextClicked.InvokeAsync();
    }

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        _timetableManager.CurrentDate = DateHelper.GetDateForDay(_timetableManager.CurrentDate, dayOfWeek, TimetableConfig.Days.First());
        _timetableManager.DisplayType = DisplayType.Day;
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
        _timetableManager.Grid = GenerateGrid();
    }

    private Grid<TEvent> GenerateGrid()
    {
        var displayService = DisplayServices.FirstOrDefault(s => s.DisplayType == _timetableManager.DisplayType)
            ?? throw new NotSupportedException($"Implementation for {nameof(DisplayType)}: '{_timetableManager.DisplayType}' not found.");

        return displayService.CreateGrid(Events, TimetableConfig, _timetableManager.CurrentDate, _eventProps);
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

    private void HandleOpenCreateModal(DateTime dateTime)
    {
        var newEvent = Activator.CreateInstance<TEvent>();

        _eventProps.SetTitle(newEvent, string.Empty);
        _eventProps.SetDateFrom(newEvent, dateTime);
        _eventProps.SetDateTo(newEvent, dateTime.AddHours(1));

        var wrapper = new EventWrapper<TEvent>()
        {
            Event = newEvent,
            GroupIdentifier = null,
            Props = _eventProps,
            Span = 1
        };

        var createFields = (RenderFragment<TEvent>)(tEvent =>
            builder =>
            {
                CreateTemplate?.Invoke(tEvent)(builder);
            });

        var onSaveCallback = EventCallback.Factory.Create(this, async (IList<TEvent> ev) =>
        {
            foreach (var e in ev)
            {
                Events.Add(e);
            }

            if (ev.Count == 1)
            {
                await OnEventCreated.InvokeAsync(ev[0]);
            }
            else
            {
                await OnGroupEventCreated.InvokeAsync(ev);
            }

            _timetableManager.Grid = GenerateGrid();
        });

        var parameters = new Dictionary<string, object>
        {
            { "EventWrapper", wrapper },
            { "Props", _eventProps },
            { "OnSave", onSaveCallback },
            { "CreateFields", createFields }
        };

        ModalService.Show<CreateEventModal<TEvent>>("Create New Event", parameters);
    }
}
