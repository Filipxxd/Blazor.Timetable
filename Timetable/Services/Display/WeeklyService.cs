using Timetable.Common;
using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class WeeklyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Week;

    public Grid<TEvent> CreateGrid<TEvent>(
        IList<TEvent> events,
        TimetableConfig config,
        DateOnly currentDate,
        CompiledProps<TEvent> props) where TEvent : class
    {
        var cellDates = CalculateGridDates(currentDate, config.Days);
        var gridStart = cellDates.First();
        var gridEndDate = cellDates.Last();

        var columns = cellDates.Select((cellDate, index) =>
        {
            var isGridFirstCell = cellDate == gridStart;

            var headerEvents = events.Where(e =>
            {
                var eventStart = props.GetDateFrom(e);
                var eventEnd = props.GetDateTo(e);

                var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

                var dateStart = eventStart.ToDateOnly();
                var dateEnd = eventEnd.ToDateOnly();

                var isOutOfTimeRange = timeStart < config.TimeFrom && timeEnd <= config.TimeFrom;
                var isMultiDay = eventStart.Day != eventEnd.Day;

                var startsInPreviousView = isMultiDay && dateStart < currentDate && dateEnd <= currentDate && dateStart < gridStart;
                var startsInthisCell = dateStart == cellDate;

                var isInThisCell = eventEnd > cellDate.ToDateTimeMidnight();

                if (props.GetTitle(e).StartsWith("Foot"))
                {

                }

                return ((isOutOfTimeRange || isMultiDay) && startsInthisCell) || (isMultiDay && startsInPreviousView && isGridFirstCell && isInThisCell);
            })
            .Select(e =>
            {
                var eventStart = props.GetDateFrom(e);
                var eventEnd = props.GetDateTo(e);
                var overlapStart = eventStart > gridStart.ToDateTimeMidnight() ? eventStart : gridStart.ToDateTimeMidnight();
                var overlapEnd = eventEnd < gridEndDate.ToDateTimeMidnight() ? eventEnd : gridEndDate.ToDateTimeMidnight().AddDays(1);

                var totalDays = (overlapEnd - overlapStart).TotalDays;

                var spanDays = totalDays == Math.Floor(totalDays) ? Math.Floor(totalDays) : Math.Ceiling(totalDays);

                var overlapDays = Math.Max((int)spanDays, 1);
                var currentDayIndex = config.Days.IndexOf(cellDate.DayOfWeek);
                var maxSpan = config.Days.Count - currentDayIndex;

                return new EventWrapper<TEvent>
                {
                    Props = props,
                    Event = e,
                    Span = Math.Min(overlapDays, maxSpan)
                };
            })
            .OrderByDescending(e => e.Span).ToList();

            var cells = new List<Cell<TEvent>>
            {
                new() {
                    DateTime = cellDate.ToDateTimeMidnight(),
                    Type = CellType.Header,
                    RowIndex = 1,
                    Events = headerEvents
                }
            };

            var timeSlots = GetTimeSlots(config.TimeFrom, config.TimeTo);

            for (var i = 0; i < timeSlots.Count; i++)
            {
                var timeSlot = timeSlots[i];

                var cellStartTime = cellDate.ToDateTimeMidnight().AddHours(timeSlot.Hour).AddMinutes(timeSlot.Minute);

                var cellEvents = events.Where(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    var eventEnd = props.GetDateTo(e);

                    var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                    var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

                    var dateStart = eventStart.ToDateOnly();
                    var dateEnd = eventEnd.ToDateOnly();

                    var isInTimeRange = timeStart >= config.TimeFrom && timeEnd <= config.TimeTo;
                    var isSameDay = dateStart.Day == dateStart.Day;
                    var fitsCellDateTime = timeStart.Hour == timeSlot.Hour && timeStart.Minute == timeSlot.Minute && dateStart.Day == cellStartTime.Day && dateStart.Month == cellStartTime.Month && dateStart.Year == cellStartTime.Year;

                    return isInTimeRange && isSameDay && fitsCellDateTime;
                })
                .Select(e =>
                {
                    var eventStart = props.GetDateFrom(e);
                    var eventEnd = props.GetDateTo(e);

                    var timeStart = new TimeOnly(eventStart.Hour, eventStart.Minute);
                    var timeEnd = new TimeOnly(eventEnd.Hour, eventEnd.Minute);

                    var span = 0;

                    while (timeStart < config.TimeTo && timeStart < timeEnd)
                    {
                        timeStart = timeStart.AddMinutes(TimetableConstants.TimeSlotInterval);
                        span++;
                    }

                    return new EventWrapper<TEvent>
                    {
                        Props = props,
                        Event = e,
                        Span = span
                    };
                })
                .OrderByDescending(e => (e.DateTo - e.DateFrom).TotalHours).ToList();

                cells.Add(new Cell<TEvent>
                {
                    DateTime = cellStartTime,
                    Type = CellType.Normal,
                    RowIndex = i + (timeSlot.Minute % TimetableConstants.TimeSlotInterval) + 2,
                    Events = cellEvents
                });
            }

            return new Column<TEvent>
            {
                DayOfWeek = cellDate.DayOfWeek,
                Index = index + 1,
                Cells = cells
            };
        }).ToList();

        var rowTitles = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")).ToList();

        return new Grid<TEvent>
        {
            Title = $"{gridStart:dddd d. MMMM} - {gridEndDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = columns
        };
    }

    private static List<TimeOnly> GetTimeSlots(TimeOnly start, TimeOnly end)
    {
        var timeSlots = new List<TimeOnly>();

        var current = start;
        while (current < end)
        {
            timeSlots.Add(current);
            current = current.AddMinutes(TimetableConstants.TimeSlotInterval);

            if (current > end)
                break;
        }

        return timeSlots;
    }

    private static List<DateOnly> CalculateGridDates(DateOnly currentDate, IEnumerable<DayOfWeek> configuredDays)
    {
        var orderedDays = configuredDays.OrderBy(d => d);
        var startDate = DateHelper.GetStartOfWeekDate(currentDate, orderedDays.First());
        var dates = new List<DateOnly> { startDate };
        var previousDate = startDate;
        var previousDayValue = (int)orderedDays.First();

        foreach (var day in orderedDays.Skip(1))
        {
            var diff = ((int)day - previousDayValue + 7) % 7;
            diff = diff == 0 ? 7 : diff;
            var nextDate = previousDate.AddDays(diff);
            dates.Add(nextDate);
            previousDayValue = (int)day;
            previousDate = nextDate;
        }
        return dates;
    }
}