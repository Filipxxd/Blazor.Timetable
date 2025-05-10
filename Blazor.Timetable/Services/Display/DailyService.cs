using Blazor.Timetable.Common;
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

        var headerItems = events
            .Where(timetableEvent =>
            {
                var eventStart = props.GetDateFrom(timetableEvent);
                var eventEnd = props.GetDateTo(timetableEvent);
                var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
                var dateStart = eventStart.ToDateOnly();

                var spansMultipleDays = eventStart.Day != eventEnd.Day;
                var outOfRange = timeStart < config.TimeFrom || timeEnd > config.TimeTo;

                return (spansMultipleDays && dateStart < date || dateStart == date)
                    && (outOfRange || spansMultipleDays);
            })
            .Select(timetableEvent => new CellItem<TEvent>
            {
                EventDescriptor = new EventDescriptor<TEvent>(timetableEvent, props),
                Span = 1
            })
            .OrderByDescending(ci => (ci.EventDescriptor.DateTo - ci.EventDescriptor.DateFrom).TotalHours)
            .ToList();

        var headerCell = new Cell<TEvent>()
        {
            DateTime = date.ToDateTimeMidnight(),
            Type = CellType.Header,
            RowIndex = 1,
            Items = headerItems
        };

        column.Cells.Add(headerCell);

        var timeSlots = DisplayServiceHelper.GetTimeSlots(config.TimeFrom, config.TimeTo);
        var midnight = date.ToDateTimeMidnight();

        var regularCells = timeSlots.Select((slotTime, slotIndex) =>
        {
            var cellTime = midnight
                .AddHours(slotTime.Hour)
                .AddMinutes(slotTime.Minute);

            var cellItems = events
                .Where(timetableEvent =>
                {
                    var eventStart = props.GetDateFrom(timetableEvent);
                    var eventEnd = props.GetDateTo(timetableEvent);

                    var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                    var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
                    var dateStart = eventStart.ToDateOnly();

                    return timeStart >= config.TimeFrom
                        && timeEnd <= config.TimeTo
                        && dateStart == date
                        && timeStart.Hour == slotTime.Hour
                        && timeStart.Minute == slotTime.Minute;
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
                RowIndex = slotIndex + slotTime.Minute % TimetableConstants.TimeSlotInterval + 2,
                Items = cellItems
            };
        }).ToList();

        column.Cells.AddRange(regularCells);

        var title = string.Format(CultureConfig.CultureInfo, "{0:dddd d. MMMM yyyy}", date);

        return new Grid<TEvent>
        {
            Title = title.CapitalizeWords(),
            RowTitles = DisplayServiceHelper.GetRowTitles(config.TimeFrom, config.TimeTo, config.Is24HourFormat),
            Columns = [column]
        };
    }
}