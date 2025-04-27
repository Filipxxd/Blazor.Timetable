using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Components.Shared.Modals;
using Timetable.Configuration;
using Timetable.Models;
using Timetable.Models.Props;
using Timetable.Services;
using Timetable.Services.DataExchange.Export;
using Timetable.Services.Display;

namespace Timetable.Components;

public partial class Timetable<TEvent> : IAsyncDisposable where TEvent : class
{
    private bool _firstRender = false;
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private CompiledProps<TEvent> _eventProps = default!;
    private IJSObjectReference _jsModule = default!;

    [Inject] internal IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] internal IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;
    [Inject] internal ModalService ModalService { get; set; } = default!;

    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<IList<TEvent>> EventsChanged { get; set; } = default!;

    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, object?>> GroupId { get; set; } = default!;
    [Parameter] public IList<EventProperty<TEvent>> AdditionalProps { get; set; } = [];

    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter] public StyleConfig StyleConfig { get; set; } = new();
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;

    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;

    #region State Change
    [Parameter] public EventCallback OnPreviousClicked { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnTitleClicked { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback<TEvent> OnEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<DayOfWeek> OnChangedToDay { get; set; } = default!;
    #endregion

    protected override void OnInitialized()
    {
        _firstRender = true;
        _objectReference = DotNetObjectReference.Create(this);

        _eventProps = new CompiledProps<TEvent>(DateFrom, DateTo, Title, GroupId, AdditionalProps);
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
            _timetableManager.CurrentDate = DateTime.Now.ToDateOnly(); // TODO: add option to provide custom via _firstRender prop;

            while (!_timetableManager.CurrentDate.IsValidDate(TimetableConfig.Days, TimetableConfig.Months))
                _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetNextValidDate(TimetableConfig.Days, TimetableConfig.Months);
        }

        TimetableConfig.Validate();
        ExportConfig.Validate();
        StyleConfig.Validate();

        UpdateGrid();
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
        if (timetableEvent is not null)
        {
            await OnEventChanged.InvokeAsync(timetableEvent);
        }

        UpdateGrid();
    }

    private async Task HandleNextClicked()
    {
        _timetableManager.NextDate(TimetableConfig);
        await OnNextClicked.InvokeAsync();
        UpdateGrid();
    }

    private async Task HandleEventUpdated(UpdateProps<TEvent> props)
    {
        if (props.Scope != ActionScope.Current)
        {
            var updatedEvents = _timetableManager.UpdateEvents(props);
            await OnGroupEventChanged.InvokeAsync(updatedEvents);
        }
        else
        {
            var updatedEvent = _timetableManager.UpdateEvent(props);
            await OnEventChanged.InvokeAsync(updatedEvent);
        }

        UpdateGrid();
    }

    private async Task HandlePreviousClicked()
    {
        _timetableManager.PreviousDate(TimetableConfig);
        await OnPreviousClicked.InvokeAsync();
        UpdateGrid();
    }

    private async Task HandleDisplayTypeChanged(DisplayType displayType)
    {
        _timetableManager.DisplayType = displayType;
        // TODO: default via param
        _timetableManager.CurrentDate = DateTime.Now.ToDateOnly();

        await OnNextClicked.InvokeAsync();
    }

    private async Task HandleChangedToDay(DayOfWeek dayOfWeek)
    {
        _timetableManager.CurrentDate = DateHelper.GetDateForDay(_timetableManager.CurrentDate, dayOfWeek, TimetableConfig.Days.First());
        _timetableManager.DisplayType = DisplayType.Day;
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
        UpdateGrid();
    }

    private void HandleOpenCreateModal(DateTime dateTime)
    {
        if (_timetableManager.DisplayType == DisplayType.Month && dateTime.Month != _timetableManager.CurrentDate.Month)
            return;

        var newEvent = Activator.CreateInstance<TEvent>();

        _eventProps.SetTitle(newEvent, string.Empty);
        _eventProps.SetDateFrom(newEvent, dateTime);
        _eventProps.SetDateTo(newEvent, dateTime.AddMinutes(TimetableConstants.TimeSlotInterval));

        var wrapper = new EventWrapper<TEvent>()
        {
            Event = newEvent,
            GroupIdentifier = null,
            Props = _eventProps,
            Span = 0
        };

        var onSaveCallback = EventCallback.Factory.Create(this, async (IList<TEvent> ev) =>
        {
            foreach (var e in ev)
            {
                Events.Add(e);
            }

            if (ev.Count == 1)
            {
                await OnEventChanged.InvokeAsync(ev[0]);
            }
            else
            {
                await OnGroupEventChanged.InvokeAsync(ev);
            }

            UpdateGrid();
        });

        var parameters = new Dictionary<string, object>
        {
            { "EventWrapper", wrapper },
            { "OnSave", onSaveCallback },
            { "AdditionalFields", AdditionalFields }
        };

        ModalService.Show<CreateEventModal<TEvent>>("Add", parameters);
    }

    private void UpdateGrid()
    {
        var displayService = DisplayServices.FirstOrDefault(s => s.DisplayType == _timetableManager.DisplayType)
            ?? throw new NotSupportedException($"Implementation for {nameof(DisplayType)}: '{_timetableManager.DisplayType}' not found.");

        _timetableManager.Events = Events;
        _timetableManager.Grid = displayService.CreateGrid(Events, TimetableConfig, _timetableManager.CurrentDate, _eventProps);
        StateHasChanged();
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
