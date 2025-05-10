using Blazor.Timetable.Common;
using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Common.Helpers;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Models.Grid;

namespace Blazor.Timetable.Services.Display;

internal sealed class WeeklyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Week;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly date,
        PropertyAccessors<TEvent> props) where TEvent : class
    {
        var weekDates = CalculateGridDates(date, config.Days);
        var weekStart = weekDates.First();
        var weekEnd = weekDates.Last();

        var columns = weekDates.Select((columnDate, columnIndex) =>
        {
            var isFirstColumn = columnDate == weekStart;

            var headerItems = events
                .Where(timetableEvent =>
                {
                    var eventStart = props.GetDateFrom(timetableEvent);
                    var eventEnd = props.GetDateTo(timetableEvent);
                    var startTime = new TimeOnly(eventStart.Hour, eventStart.Minute);
                    var endTime = new TimeOnly(eventEnd.Hour, eventEnd.Minute);
                    var startDate = eventStart.ToDateOnly();
                    var endDate = eventEnd.ToDateOnly();

                    //var isSingleDayOutsideTimeRange = startDate == columnDate && startDate == endDate && (startTime < config.TimeFrom || endTime > config.TimeTo);
                    //var isMultiDay = startDate == columnDate && startDate != endDate;

                    var outOfRange = startTime > config.TimeTo || endTime > config.TimeTo || startTime < config.TimeFrom;
                    var spansMultipleDays = eventStart.Day != eventEnd.Day;
                    var startedBefore = spansMultipleDays
                        && startDate < date
                        && endDate <= date
                        && startDate < weekStart;
                    var startsToday = startDate == columnDate;
                    var continuesIntoColumn = eventEnd > columnDate.ToDateTimeMidnight();

                    return ((outOfRange || spansMultipleDays) && startsToday) || (spansMultipleDays && startedBefore && isFirstColumn && continuesIntoColumn);
                })
                .Select(timetableEvent =>
                {
                    var eventStart = props.GetDateFrom(timetableEvent);
                    var eventEnd = props.GetDateTo(timetableEvent);
                    var overlapStart = eventStart > weekStart.ToDateTimeMidnight()
                        ? eventStart
                        : weekStart.ToDateTimeMidnight();
                    var overlapEnd = eventEnd < weekEnd.ToDateTimeMidnight()
                        ? eventEnd
                        : weekEnd.ToDateTimeMidnight().AddDays(1);

                    var totalDays = (overlapEnd - overlapStart).TotalDays;
                    var spanDays = (int)(totalDays == Math.Floor(totalDays)
                        ? totalDays
                        : Math.Ceiling(totalDays));
                    var maxSpan = config.Days.Count - config.Days.IndexOf(columnDate.DayOfWeek);

                    return new CellItem<TEvent>
                    {
                        EventDescriptor = new EventDescriptor<TEvent>(timetableEvent, props),
                        Span = Math.Min(Math.Max(spanDays, 1), maxSpan)
                    };
                })
                .OrderByDescending(ci => ci.Span)
                .ToList();

            var cells = new List<Cell<TEvent>>
            {
                new()
                {
                    DateTime = columnDate.ToDateTimeMidnight(),
                    Type = CellType.Header,
                    RowIndex = 1,
                    Items = headerItems
                }
            };

            var timeSlots = DisplayServiceHelper.GetTimeSlots(config.TimeFrom, config.TimeTo);
            var midnight = columnDate.ToDateTimeMidnight();

            for (var i = 0; i < timeSlots.Count; i++)
            {
                var slotTime = timeSlots[i];
                var cellDateTime = midnight
                    .AddHours(slotTime.Hour)
                    .AddMinutes(slotTime.Minute);

                var cellItems = events
                    .Where(timetableEvent =>
                    {
                        var eventStart = props.GetDateFrom(timetableEvent).ToDateOnly();
                        var eventEnd = props.GetDateTo(timetableEvent).ToDateOnly();
                        var timeStart = props.GetDateFrom(timetableEvent).ToTimeOnly();
                        var timeEnd = props.GetDateTo(timetableEvent).ToTimeOnly();

                        return timeStart >= config.TimeFrom
                            && timeEnd <= config.TimeTo
                            && eventStart == eventEnd
                            && eventStart == columnDate
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

                cells.Add(new Cell<TEvent>
                {
                    DateTime = cellDateTime,
                    Type = CellType.Normal,
                    RowIndex = i + (slotTime.Minute % TimetableConstants.TimeSlotInterval) + 2,
                    Items = cellItems
                });
            }

            return new Column<TEvent>
            {
                DayOfWeek = columnDate.DayOfWeek,
                Index = columnIndex + 1,
                Cells = cells
            };
        }).ToList();

        return new Grid<TEvent>
        {
            Title = GetTitle(weekStart, weekEnd).CapitalizeWords(),
            RowTitles = DisplayServiceHelper.GetRowTitles(config.TimeFrom, config.TimeTo, config.Is24HourFormat),
            Columns = columns
        };
    }

    private static List<DateOnly> CalculateGridDates(DateOnly date, IEnumerable<DayOfWeek> days)
    {
        var orderedDays = days.OrderBy(d => d).ToArray();
        var startOfWeek = DateTimeHelper.GetStartOfWeekDate(date, orderedDays[0]);
        var dates = new List<DateOnly> { startOfWeek };
        var previousDate = startOfWeek;
        var previousDayValue = (int)orderedDays[0];

        foreach (var day in orderedDays.Skip(1))
        {
            var diff = ((int)day - previousDayValue + 7) % 7;
            if (diff == 0) diff = 7;
            previousDate = previousDate.AddDays(diff);
            previousDayValue = (int)day;
            dates.Add(previousDate);
        }

        return dates;
    }

    private static string GetTitle(DateOnly weekStart, DateOnly weekEnd)
    {
        var pattern = weekStart.Month == weekEnd.Month
            ? "{0:dddd d.} - {1:dddd d., MMMM yyyy}"
            : "{0:dddd d., MMMM} - {1:dddd d., MMMM yyyy}";
        return string.Format(CultureConfig.CultureInfo, pattern, weekStart, weekEnd);
    }
}