using Blazor.Timetable.Common;
using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Exceptions;

namespace Blazor.Timetable.Models.Configuration;

public sealed class TimetableConfig
{
    /// <summary>
    /// Months shown in the timetable. Must be consecutive. First item treated as start of year month. Defaults to all months of the year.
    /// </summary>
    public IList<Month> Months { get; init; } = [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June,
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];

    /// <summary>
    /// Days shown in the timetable. Must be consecutive. First item treated as start of week day. Defaults to all days of the week.
    /// </summary>
    public IList<DayOfWeek> Days { get; init; } = [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    /// <summary>
    /// Start time for displayed events. Defaults to 08:00 (8AM).
    /// </summary>
    public TimeOnly TimeFrom { get; init; } = new(8, 0);

    /// <summary>
    /// End time for displayed events. Defaults to 20:00 (8PM).
    /// </summary>
    public TimeOnly TimeTo { get; init; } = new(20, 0);

    /// <summary>
    /// Use 24-hour format for display times. Defaults to <see cref="true"/>.
    /// </summary>
    public bool Is24HourFormat { get; init; } = true;

    /// <summary>
    /// Initial display type of the timetable on component initialization. Defaults to <see cref="DisplayType.Week"/>.
    /// </summary>
    public DisplayType DisplayType { get; init; } = DisplayType.Week;

    internal void Validate()
    {
        if (TimeTo <= TimeFrom && TimeTo < TimetableConstants.EndOfDay)
            throw new InvalidSetupException($"{nameof(TimeTo)} must be greater than ${nameof(TimeFrom)}.");

        if (TimeFrom.Minute % TimetableConstants.TimeSlotInterval != 0)
            throw new InvalidOperationException($"{nameof(TimeFrom)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");

        if (TimeTo.Minute % TimetableConstants.TimeSlotInterval != 0 && TimeTo < TimetableConstants.EndOfDay)
            throw new InvalidOperationException($"{nameof(TimeTo)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");

        if (Months.Count == 0)
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");

        if (Months.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(Months)}.");

        if (!Days.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");

        if (Days.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(Days)}.");

        if (!AreDaysConsecutive(Days))
            throw new InvalidSetupException($"Days must be consecutive.");

        if (!AreMonthsConsecutive(Months))
            throw new InvalidSetupException($"Months must be consecutive.");
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
    private static bool AreMonthsConsecutive(IEnumerable<Month> months)
    {
        var sortedMonths = months.OrderBy(m => m).ToList();
        for (int i = 0; i < sortedMonths.Count - 1; i++)
        {
            if (((int)sortedMonths[i] + 1) % 12 != ((int)sortedMonths[i + 1]) % 12)
            {
                return false;
            }
        }
        return true;
    }
}