namespace Timetable.Common.Helpers;

internal static class CalendarHelper
{
    public static List<List<DateOnly>> CalculateMonthGridDates(
        DateOnly currentDate,
        IList<DayOfWeek> configuredDays)
    {
        if (configuredDays == null || configuredDays.Count == 0)
            throw new ArgumentException("configuredDays must contain at least one element.");

        var firstOfMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var startDiff = ((int)firstOfMonth.DayOfWeek - (int)configuredDays[0] + 7) % 7;
        var gridStartBase = firstOfMonth.AddDays(-startDiff);
        var offsets = configuredDays
                              .Select(d => ((int)d - (int)configuredDays[0] + 7) % 7)
                              .ToArray();

        var maxWeekIndex = (lastOfMonth.DayNumber - gridStartBase.DayNumber) / 7;

        var firstWeekIndex = 0;
        while (firstWeekIndex <= maxWeekIndex)
        {
            var ws = gridStartBase.AddDays(firstWeekIndex * 7);
            if (offsets.Any(off => ws.AddDays(off).Month == currentDate.Month))
                break;
            firstWeekIndex++;
        }

        var rows = new List<List<DateOnly>>();
        for (var wi = firstWeekIndex; wi <= maxWeekIndex; wi++)
        {
            var ws = gridStartBase.AddDays(wi * 7);
            var row = offsets.Select(off => ws.AddDays(off)).ToList();
            rows.Add(row);
        }

        return rows;
    }
}
