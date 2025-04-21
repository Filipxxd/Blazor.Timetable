using Timetable.Common.Enums;
using Timetable.Common.Exceptions;

namespace Timetable.Configuration;

public sealed class TimetableConfig
{
    /// <summary>
    /// Months shown in the timetable. Must be consecutive. First item treated as start of year month. Defaults to all months of the year.
    /// </summary>
    public IEnumerable<Month> Months { get; init; } = [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June,
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];

    /// <summary>
    /// Days shown in the timetable. Must be consecutive. First item treated as start of week day. Defaults to all days of the week.
    /// </summary>
    public IEnumerable<DayOfWeek> Days { get; init; } = [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    /// <summary>
    /// Allowed display modes for the timetable. Defaults to <see cref="DisplayType.Day"/>, <see cref="DisplayType.Week"/>, <see cref="DisplayType.Month"/>.
    /// </summary>
    public IEnumerable<DisplayType> DisplayTypes { get; init; } = [DisplayType.Day, DisplayType.Week, DisplayType.Month];

    /// <summary>
    /// Start time for displayed events. Defaults to 00:00 (0AM).
    /// </summary>
    public TimeOnly TimeFrom { get; init; } = new(0, 0);

    /// <summary>
    /// End time for displayed events. Defaults to 23:00 (11PM).
    /// </summary>
    public TimeOnly TimeTo { get; init; } = new(23, 0);

    /// <summary>
    /// Use 24-hour format for display times. Defaults to <see cref="true"/>.
    /// </summary>
    public bool Is24HourFormat { get; init; } = true;

    /// <summary>
    /// Initial display type of the timetable. Defaults to <see cref="DisplayType.Week"/>.
    /// </summary>
    public DisplayType DefaultDisplayType { get; set; } = DisplayType.Week;

    internal IEnumerable<int> Hours => Enumerable.Range(TimeFrom.Hour, TimeTo.Hour - TimeFrom.Hour);

    internal void Validate()
    {
        if (TimeTo <= TimeFrom)
            throw new InvalidSetupException($"{nameof(TimeTo)} must be greater than ${nameof(TimeFrom)}.");

        if (TimeFrom.Minute % 15 != 0)
            throw new InvalidOperationException($"{nameof(TimeFrom)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");

        if (TimeTo.Minute % 15 != 0)
            throw new InvalidOperationException($"{nameof(TimeTo)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");

        if (!DisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(DisplayType)} in {nameof(DisplayTypes)} required.");

        if (DisplayTypes.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate {nameof(DisplayType)} found in {nameof(DisplayTypes)}.");

        if (!DisplayTypes.Contains(DefaultDisplayType))
            throw new InvalidSetupException($"{nameof(DisplayType)} must be part of {nameof(DisplayTypes)}.");

        if (!Months.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");

        if (Months.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(Months)}.");

        if (!Days.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");

        if (Days.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(Days)}.");

        if (!AreDaysConsecutive(Days))
            throw new InvalidSetupException($"Days must be consecutive.");
    }

    private static bool AreDaysConsecutive(IEnumerable<DayOfWeek> days)
    {
        var sortedDays = days.OrderBy(d => d).ToList();

        for (int i = 0; i < sortedDays.Count - 1; i++)
        {
            if (((int)sortedDays[i] + 1) % 7 != (int)sortedDays[i + 1] % 7)
            {
                return false;
            }
        }
        return true;
    }
}