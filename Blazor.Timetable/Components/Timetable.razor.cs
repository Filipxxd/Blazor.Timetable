using Blazor.Timetable.Common;
using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Common.Helpers;
using Blazor.Timetable.Components.Modals;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Models.Grid;
using Blazor.Timetable.Services;
using Blazor.Timetable.Services.Display;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Expressions;

namespace Blazor.Timetable.Components;

public partial class Timetable<TEvent> : IAsyncDisposable where TEvent : class
{
    private bool _firstRender = false;
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private PropertyAccessors<TEvent> _eventProps = default!;
    private IJSObjectReference _jsModule = default!;

    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;
    [Inject] private ModalService ModalService { get; set; } = default!;
    [Inject] private Localizer L { get; set; } = default!;

    [Parameter, EditorRequired] public IList<TEvent> Events { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<IList<TEvent>> EventsChanged { get; set; } = default!;

    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateFrom { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, DateTime>> DateTo { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string>> Title { get; set; } = default!;
    [Parameter, EditorRequired] public Expression<Func<TEvent, string?>> GroupId { get; set; } = default!;
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
    [Parameter] public EventCallback<DayOfWeek> OnChangedToDay { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventCreated { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventCreated { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventChanged { get; set; } = default!;
    [Parameter] public EventCallback<TEvent> OnEventDeleted { get; set; } = default!;
    [Parameter] public EventCallback<IList<TEvent>> OnGroupEventDeleted { get; set; } = default!;
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

        var selectors = new List<ISelector<TEvent>> {
            new Selector<TEvent, DateTime>("DateFrom", DateFrom),
            new Selector<TEvent, DateTime>("DateTo", DateTo),
            new Selector<TEvent, string>("Title", Title!),
            new Selector<TEvent, string>("GroupIdentifier", GroupId)
        };

        ExportConfig = new ExportConfig<TEvent>
        {
            Selectors = selectors
        };

        ImportConfig = new ImportConfig<TEvent>
        {
            Selectors = selectors
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
        ImportConfig.Validate();
        StyleConfig.Validate();

        UpdateGrid();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Blazor.Timetable/Components/Timetable.razor.js");
            await _jsModule.InvokeVoidAsync("dragDrop.init", _objectReference);
            _firstRender = false;
        }
    }

    [JSInvokable]
    public async Task MoveEvent(Guid eventId, Guid targetCellId)
    {
        var eventItem = _timetableManager.Grid.FindItemByItemId(eventId);

        if (eventItem is null)
            return;

        if (eventItem.EventDescriptor.HasGroupdAssigned)
        {
            var handleMoveSingle = EventCallback.Factory.Create(this, async () =>
            {
                var timetableEvent = _timetableManager.MoveEvent(eventId, targetCellId);
                if (timetableEvent is not null)
                {
                    await OnEventChanged.InvokeAsync(timetableEvent);
                }
                UpdateGrid();
            });

            var handleMoveGroup = EventCallback.Factory.Create(this, async () =>
            {
                var timetableEvents = _timetableManager.MoveEventGroup(Events, eventId, targetCellId);
                if (timetableEvents?.Count != 0)
                {
                    await OnGroupEventChanged.InvokeAsync(timetableEvents);
                }
                UpdateGrid();
            });

            var parameters = new Dictionary<string, object>
            {
                { "OnSingleMove", handleMoveSingle },
                { "OnGroupMove", handleMoveGroup },
                { "OnCancel", EventCallback.Factory.Create(this, UpdateGrid)}
            };

            ModalService.Show<GroupMoveModal>("Move", parameters);
        }
        else
        {
            var timetableEvent = _timetableManager.MoveEvent(eventId, targetCellId);
            if (timetableEvent is not null)
            {
                await OnEventChanged.InvokeAsync(timetableEvent);
            }

            UpdateGrid();
        }
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

    private async Task HandleEventUpdated(UpdateAction<TEvent> props)
    {
        if (props.Scope == ActionScope.Single)
        {
            var updatedEvent = _timetableManager.UpdateEvent(props);
            await OnEventChanged.InvokeAsync(updatedEvent);
        }
        else
        {
            var updatedEvents = _timetableManager.UpdateGroupEvent(Events, props);
            await OnGroupEventChanged.InvokeAsync(updatedEvents);
        }

        UpdateGrid();
    }

    private void HandleEventDeleted(DeleteAction<TEvent> deleteProps)
    {
        if (deleteProps.Scope == ActionScope.Single)
        {
            var deletedEvent = _timetableManager.DeleteEvent(Events, deleteProps);
            OnEventDeleted.InvokeAsync(deletedEvent);
        }
        else
        {
            var deletedEvents = _timetableManager.DeleteGroupEvent(Events, deleteProps);
            OnGroupEventDeleted.InvokeAsync(deletedEvents);
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
        _timetableManager.CurrentDate = DateTimeHelper.GetDateForDay(_timetableManager.CurrentDate, dayOfWeek, TimetableConfig.Days.First());
        _timetableManager.DisplayType = DisplayType.Day;
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
        UpdateGrid();
    }

    private void HandleImport(ImportAction<TEvent> props)
    {
        if (props.Type == ImportType.Append)
        {
            foreach (var item in props.Events)
                Events.Add(item);
        }
        else
        {
            Events = props.Events;
        }

        UpdateGrid();
    }

    private void HandleOpenCreateModal(DateTime cellDate)
    {
        if (_timetableManager.DisplayType == DisplayType.Month && cellDate.Month != _timetableManager.CurrentDate.Month)
            return;

        var template = Activator.CreateInstance<TEvent>();
        _eventProps.SetDateFrom(template, cellDate);
        _eventProps.SetDateTo(template, cellDate.AddMinutes(TimetableConstants.TimeSlotInterval));
        _eventProps.SetTitle(template, string.Empty);

        var templateDesc = new EventDescriptor<TEvent>(template, _eventProps);

        var onCreate = EventCallback.Factory.Create<CreateAction<TEvent>>(this, async props =>
        {
            var created = _timetableManager.CreateEvents(templateDesc, props.Repetition, props.RepeatUntil, props.RepeatDays);

            foreach (var e in created)
                Events.Add(e);

            if (created.Count == 1)
                await OnEventCreated.InvokeAsync(created[0]);
            else
                await OnGroupEventCreated.InvokeAsync(created);

            UpdateGrid();
        });

        var parameters = new Dictionary<string, object>
        {
            { "OriginalEventDescriptor", templateDesc },
            { "State", EventModalState.Create },
            { "OnCreate", onCreate },
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
