using Timetable.Common.Enums;
using Timetable.Common.Extensions;
using Timetable.Common.Helpers;
using Timetable.Configuration;
using Timetable.Models;

namespace Timetable.Services.Display;

internal sealed class DailyService : IDisplayService
{
    public DisplayType DisplayType => DisplayType.Day;

    public Grid<TEvent> CreateGrid<TEvent>(
            IList<TEvent> events,
            TimetableConfig config,
            DateTime currentDate,
            CompiledProps<TEvent> props) where TEvent : class
    {
        var cellDate = currentDate.Date;

        var todayEvents = events.Where(e =>
        {
            var eventStart = props.GetDateFrom(e);
            return eventStart >= cellDate && eventStart < cellDate.AddDays(1);
        }).ToList();

        var rowTitles = config.Hours.Select(hour =>
            config.Is24HourFormat
                ? TimeSpan.FromHours(hour).ToString(@"hh\:mm")
                : DateTime.Today.AddHours(hour).ToString("h tt")
        ).ToList();

        var dayIndex = 1;
        var columns = new List<Column<TEvent>>();

        var column = new Column<TEvent>
        {
            DayOfWeek = cellDate.DayOfWeek,
            Index = dayIndex,
            Cells = TimetableHelper.CreateCells(cellDate, config, todayEvents, props)
        };
        columns.Add(column);
        dayIndex++;

        return new Grid<TEvent>
        {
            Title = $"{cellDate:dddd d. MMMM yyyy}".CapitalizeWords(),
            RowTitles = rowTitles,
            Columns = columns
        };
    }
}