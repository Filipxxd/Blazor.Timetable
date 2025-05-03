using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Components.Shared.Modals;
using Timetable.Models;
using Timetable.Models.Configuration;
using Timetable.Models.Grid;
using Timetable.Models.Props;
using Timetable.Services;
using Timetable.Services.DataExchange.Export;
using Timetable.Services.DataExchange.Import;
using Timetable.Services.Display;

namespace Timetable.Components;

public partial class Timetable<TEvent> : IAsyncDisposable where TEvent : class
{
    private bool _firstRender = false;
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private PropertyAccessors<TEvent> _eventProps = default!;
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
    [Parameter] public IEnumerable<Expression<Func<TEvent, object?>>> AdditionalProps { get; set; } = [];

    [Parameter] public TimetableConfig TimetableConfig { get; set; } = new();
    [Parameter] public StyleConfig StyleConfig { get; set; } = new();
    [Parameter] public ExportConfig<TEvent> ExportConfig { get; set; } = default!;
    [Parameter] public ImportConfig<TEvent> ImportConfig { get; set; } = default!;

    [Parameter] public RenderFragment<TEvent> AdditionalFields { get; set; } = default!;

    #region State Change
    [Parameter] public EventCallback OnPreviousClicked { get; set; } = default!;
    [Parameter] public EventCallback OnNextClicked { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnTitleClicked { get; set; } = default!;
    [Parameter] public EventCallback<DisplayType> OnDisplayTypeChanged { get; set; }
    [Parameter] public EventCallback<TEvent> OnEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventDeleted { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventDeleted { get; set; } = default!;
    [Parameter] public EventCallback<DayOfWeek> OnChangedToDay { get; set; } = default!;
    #endregion

    protected override void OnInitialized()
    {
        _firstRender = true;
        _objectReference = DotNetObjectReference.Create(this);

        _eventProps = new PropertyAccessors<TEvent>(DateFrom, DateTo, Title, GroupId, AdditionalProps);
        _timetableManager = new TimetableManager<TEvent>()
        {
            Props = _eventProps
        };

        ExportConfig = new ExportConfig<TEvent>
        {
            FileName = "EventExport",
            Transformer = new CsvTransformer(),
            Properties = [
                new ExportSelector<TEvent, DateTime>("DateFrom", DateFrom),
                new ExportSelector<TEvent, DateTime>("DateTo", DateTo),
                new ExportSelector<TEvent, string>("Title", Title)
            ]
        };

        ImportConfig = new ImportConfig<TEvent>
        {
            AllowedExtensions = ["csv"],
            MaxFileSizeBytes = 5_000_000,
            Transformer = new CsvImportTransformer<TEvent>([
              new ImportSelector<TEvent,DateTime>("DateFrom", DateFrom),
              new ImportSelector<TEvent,DateTime>("DateTo", DateTo),
              new ImportSelector<TEvent,string>("Title", Title)
            ])
        };
    }

    protected override void OnParametersSet()
    {
        if (_firstRender)
        {
            _timetableManager.DisplayType = TimetableConfig.DefaultDisplayType;
            _timetableManager.CurrentDate = TimetableConfig.DefaultDate;

            while (!_timetableManager.CurrentDate.IsValidFor(TimetableConfig.Days, TimetableConfig.Months))
            {
                _timetableManager.CurrentDate = _timetableManager.CurrentDate.AddDays(1);
            }
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
        _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetValidDateFor(_timetableManager.DisplayType, TimetableConfig.Days, TimetableConfig.Months, true);
        await OnNextClicked.InvokeAsync();
        UpdateGrid();
    }

    private async Task HandlePreviousClicked()
    {
        _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetValidDateFor(_timetableManager.DisplayType, TimetableConfig.Days, TimetableConfig.Months, false);
        await OnPreviousClicked.InvokeAsync();
        UpdateGrid();
    }

    private async Task HandleEventUpdated(UpdateProps<TEvent> props)
    {
        if (props.Scope != ActionScope.Single)
        {
            var updatedEvents = _timetableManager.UpdateEvents(Events, props);
            await OnGroupEventChanged.InvokeAsync(updatedEvents);
        }
        else
        {
            var updatedEvent = _timetableManager.UpdateEvent(props);
            await OnEventChanged.InvokeAsync(updatedEvent);
        }

        UpdateGrid();
    }

    private void HandleEventDeleted(DeleteProps<TEvent> deleteProps)
    {
        var deleted = _timetableManager.DeleteEvent(Events, deleteProps);

        if (deleteProps.Scope == ActionScope.Single)
        {
            OnEventDeleted.InvokeAsync(deleted[0]);
        }
        else
        {
            OnGroupEventDeleted.InvokeAsync(deleted);
        }

        UpdateGrid();
    }

    private async Task HandleDisplayTypeChanged(DisplayType displayType)
    {
        _timetableManager.DisplayType = displayType;
        _timetableManager.CurrentDate = TimetableConfig.DefaultDate;

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

    private async Task HandleImport(ImportProps<TEvent> props)
    {
        if (props.Type == ImportType.Append)
        {
            foreach (var item in props.Events)
            {
                Events.Add(item);
            }
        }
        else
        {
            Events = props.Events;
        }

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

        var wrapper = new EventWrapper<TEvent>(newEvent, _eventProps)
        {
            GroupId = null
        };

        var handleCreate = EventCallback.Factory.Create(this, async (CreateProps<TEvent> props) =>
        {
            var EventWrapper = props.EventWrapper;

            var eventsToCreate = new List<TEvent>();
            var baseStart = EventWrapper.DateFrom;
            var baseEnd = EventWrapper.DateTo;

            eventsToCreate.Add(EventWrapper.Event);

            if (props.Repetition != RepeatOption.Once)
            {
                var groupId = Guid.NewGuid().ToString();
                EventWrapper.Props.SetGroupId(EventWrapper.Event, groupId);

                if (props.Repetition == RepeatOption.Custom && !props.RepeatDays.HasValue)
                    throw new Exception();

                var i = 1;
                while (true)
                {
                    DateTime offsetStart, offsetEnd;
                    switch (props.Repetition)
                    {
                        case RepeatOption.Daily:
                            offsetStart = baseStart.AddDays(1 * i);
                            offsetEnd = baseEnd.AddDays(1 * i);
                            break;
                        case RepeatOption.Weekly:
                            offsetStart = baseStart.AddDays(7 * i);
                            offsetEnd = baseEnd.AddDays(7 * i);
                            break;
                        case RepeatOption.Monthly:
                            offsetStart = baseStart.AddMonths(i);
                            offsetEnd = baseEnd.AddMonths(i);
                            break;
                        case RepeatOption.Custom:
                            offsetStart = baseStart.AddDays(props.RepeatDays!.Value * i);
                            offsetEnd = baseEnd.AddDays(props.RepeatDays.Value * i);
                            break;
                        default:
                            offsetStart = baseStart;
                            offsetEnd = baseEnd;
                            break;
                    }

                    if (offsetStart.ToDateOnly() > props.RepeatUntil)
                        break;

                    TEvent newEvent = Activator.CreateInstance<TEvent>();
                    EventWrapper.Props.SetTitle(newEvent, EventWrapper.Title);
                    EventWrapper.Props.SetDateFrom(newEvent, offsetStart);
                    EventWrapper.Props.SetDateTo(newEvent, offsetEnd);
                    EventWrapper.Props.SetGroupId(newEvent, groupId);

                    foreach (var (getter, setter) in EventWrapper.Props.AdditionalProperties)
                    {
                        var updatedValue = getter(props.EventWrapper.Event);
                        setter(newEvent, updatedValue);
                    }


                    eventsToCreate.Add(newEvent);
                    i++;
                }
            }

            foreach (var e in eventsToCreate)
            {
                Events.Add(e);
            }

            if (eventsToCreate.Count == 1)
            {
                await OnEventChanged.InvokeAsync(eventsToCreate[0]);
            }
            else
            {
                await OnGroupEventChanged.InvokeAsync(eventsToCreate);
            }

            UpdateGrid();
        });

        var parameters = new Dictionary<string, object>
        {
            { "EventWrapper", wrapper },
            { "State", EventModalState.Create },
            { "OnCreate", handleCreate },
            { "AdditionalFields", AdditionalFields }
        };

        ModalService.Show<EventModal<TEvent>>("Add", parameters);
    }

    private void UpdateGrid()
    {
        var displayService = DisplayServices.FirstOrDefault(s => s.DisplayType == _timetableManager.DisplayType)
            ?? throw new NotSupportedException($"Implementation for {nameof(DisplayType)}: '{_timetableManager.DisplayType}' not found.");

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
