using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Common.Helpers;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Services.Display;

internal sealed class DailyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Day;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly date,
        PropertyAccessors<TEvent> props) where TEvent : class
    {
        var column = new Column<TEvent>
        {
            DayOfWeek = date.DayOfWeek,
            Index = 1,
            Cells = []
        };

        var midnight = date.ToDateTimeMidnight();
        var timeSlots = DisplayServiceHelper.GetTimeSlots(config.TimeFrom, config.TimeTo);

        var relevantEvents = events
            .Where(timetableEvent =>
            {
                var eventStart = props.GetDateFrom(timetableEvent);
                var eventEnd = props.GetDateTo(timetableEvent);
                var dateStart = eventStart.ToDateOnly();
                var dateEnd = eventEnd.ToDateOnly();
                var spansMultipleDays = dateStart <= date && dateEnd >= date && dateStart != dateEnd;
                var startsOnDate = dateStart == date;

                return spansMultipleDays ||
                       (startsOnDate &&
                        new TimeOnly(eventStart.Hour, eventStart.Minute) < config.TimeTo &&
                        new TimeOnly(eventEnd.Hour, eventEnd.Minute) > config.TimeFrom);
            }).ToList();

        var headerItems = relevantEvents
            .Where(timetableEvent =>
            {
                var eventStart = props.GetDateFrom(timetableEvent);
                var eventEnd = props.GetDateTo(timetableEvent);
                var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
                var dateStart = eventStart.ToDateOnly();
                var dateEnd = eventEnd.ToDateOnly();
                var spansMultipleDays = dateStart <= date && eventEnd.ToDateOnly() >= date && dateStart != dateEnd;
                var outOfRange = timeStart < config.TimeFrom || timeEnd > config.TimeTo;

                return spansMultipleDays && (outOfRange || spansMultipleDays);
            })
            .Select(timetableEvent => new CellItem<TEvent>
            {
                EventDescriptor = new EventDescriptor<TEvent>(timetableEvent, props),
                Span = 1
            })
            .OrderByDescending(ci => (ci.EventDescriptor.DateTo - ci.EventDescriptor.DateFrom).TotalHours)
            .ToList();

        var headerCell = new Cell<TEvent>
        {
            DateTime = midnight,
            Type = CellType.Header,
            RowIndex = 1,
            Items = headerItems
        };
        column.Cells.Add(headerCell);

        var regularCells = timeSlots.Select((slotTime, slotIndex) =>
        {
            var cellTime = midnight
                .AddHours(slotTime.Hour)
                .AddMinutes(slotTime.Minute);

            var cellItems = relevantEvents
                .Where(timetableEvent =>
                {
                    var eventStart = props.GetDateFrom(timetableEvent);
                    var eventEnd = props.GetDateTo(timetableEvent);
                    var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                    var dateStart = eventStart.ToDateOnly();

                    return dateStart == date &&
                           timeStart == slotTime &&
                           timeStart >= config.TimeFrom &&
                           timeStart < config.TimeTo &&
                           eventStart.Date == eventEnd.Date;
                })
                .Select(timetableEvent => new CellItem<TEvent>
                {
                    EventDescriptor = new EventDescriptor<TEvent>(timetableEvent, props),
                    Span = DisplayServiceHelper.GetEventSpan(timetableEvent, config.TimeTo, props)
                })
                .OrderByDescending(ci => (ci.EventDescriptor.DateTo - ci.EventDescriptor.DateFrom).TotalHours)
                .ToList();

            return new Cell<TEvent>
            {
                DateTime = cellTime,
                Type = CellType.Normal,
                RowIndex = slotIndex + 2,
                Items = cellItems
            };
        }).ToList();

        column.Cells.AddRange(regularCells);

        var title = string.Format(
            CultureConfig.CultureInfo,
            "{0:dddd d. MMMM yyyy}",
            date
        );

        return new Grid<TEvent>
        {
            Title = title.CapitalizeWords(),
            RowTitles = DisplayServiceHelper.GetRowTitles(config.TimeFrom, config.TimeTo, config.Is24HourFormat),
            Columns = [column]
        };
    }
}