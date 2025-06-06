﻿using Blazor.Timetable.Common;
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
    private readonly ModalService _modalService = new();
    private DotNetObjectReference<Timetable<TEvent>> _objectReference = default!;
    private TimetableManager<TEvent> _timetableManager = default!;
    private PropertyAccessors<TEvent> _eventProps = default!;
    private IJSObjectReference _jsModule = default!;
    private bool _firstRender = false;

    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private IEnumerable<IDisplayService> DisplayServices { get; set; } = default!;

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
    [Parameter] public EventCallback<ImportAction<TEvent>> OnEventsImported { get; set; } = default!;
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
        _timetableManager = new TimetableManager<TEvent>(_eventProps);

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
            var date = DateTime.Now.ToDateOnly();
            _timetableManager.CurrentDate = DateTimeHelper.GetNextValidDate(date, TimetableConfig.Days, TimetableConfig.Months);
            _timetableManager.DisplayType = TimetableConfig.DisplayType;
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
    public async Task MoveEventAsync(Guid eventId, Guid targetCellId)
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

            _modalService.Show<GroupMoveModal>(parameters, false);
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

    private async Task HandleNextClickedAsync()
    {
        _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetValidDateFor(_timetableManager.DisplayType, TimetableConfig.Days, TimetableConfig.Months, true);
        UpdateGrid();
        await OnNextClicked.InvokeAsync();
    }

    private async Task HandlePreviousClickedAsync()
    {
        _timetableManager.CurrentDate = _timetableManager.CurrentDate.GetValidDateFor(_timetableManager.DisplayType, TimetableConfig.Days, TimetableConfig.Months, false);
        UpdateGrid();
        await OnPreviousClicked.InvokeAsync();
    }

    private async Task HandleEventUpdatedAsync(UpdateAction<TEvent> props)
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

    private async Task HandleEventDeletedAsync(DeleteAction<TEvent> deleteProps)
    {
        if (deleteProps.Scope == ActionScope.Single)
        {
            var deletedEvent = _timetableManager.DeleteEvent(Events, deleteProps);
            await OnEventDeleted.InvokeAsync(deletedEvent);
        }
        else
        {
            var deletedEvents = _timetableManager.DeleteGroupEvent(Events, deleteProps);
            await OnGroupEventDeleted.InvokeAsync(deletedEvents);
        }

        UpdateGrid();
    }

    private async Task HandleDisplayTypeChangedAsync(DisplayType displayType)
    {
        _timetableManager.DisplayType = displayType;
        _timetableManager.CurrentDate = DateTime.Now.ToDateOnly();

        UpdateGrid();
        await OnNextClicked.InvokeAsync();
    }

    private async Task HandleChangedToDayAsync(DayOfWeek dayOfWeek)
    {
        _timetableManager.CurrentDate = DateTimeHelper.GetDateForDay(_timetableManager.CurrentDate, dayOfWeek, TimetableConfig.Days.First());
        _timetableManager.DisplayType = DisplayType.Day;
        UpdateGrid();
        await OnChangedToDay.InvokeAsync(dayOfWeek);
        await OnDisplayTypeChanged.InvokeAsync(DisplayType.Day);
    }

    private async Task HandleImportAsync(ImportAction<TEvent> props)
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
        await OnEventsImported.InvokeAsync(props);
    }

    private void HandleOpenCreateModal(DateTime cellDate)
    {
        if (_timetableManager.DisplayType == DisplayType.Month && cellDate.Month != _timetableManager.CurrentDate.Month)
            return;

        var eventDescriptor = EventDescriptor<TEvent>.Create(_eventProps);
        eventDescriptor.DateFrom = cellDate;
        eventDescriptor.DateTo = cellDate.AddMinutes(TimetableConstants.TimeSlotInterval);
        eventDescriptor.Title = string.Empty;

        var onCreate = EventCallback.Factory.Create<CreateAction<TEvent>>(this, async props =>
        {
            var created = _timetableManager.CreateEvents(eventDescriptor, props.Repetition, props.RepeatUntil, props.RepeatDays);

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
            { "OriginalEventDescriptor", eventDescriptor },
            { "State", EventModalState.Create },
            { "OnCreate", onCreate },
            { "AdditionalFields", AdditionalFields }
        };

        _modalService.Show<EventModal<TEvent>>(parameters);
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
