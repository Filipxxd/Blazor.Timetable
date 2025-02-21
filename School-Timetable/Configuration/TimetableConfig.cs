using School_Timetable.Enums;
using School_Timetable.Exceptions;

namespace School_Timetable.Configuration;

public sealed class TimetableConfig
{
    public IEnumerable<Month> SupportedMonths { get; init; } =     [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June, 
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];
    public IEnumerable<DayOfWeek> SupportedDays { get; init; } =     [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];
    public IEnumerable<DisplayType> SupportedDisplayTypes { get; init; } = [DisplayType.Day, DisplayType.Week, DisplayType.Month];
    public DisplayType DefaultDisplayType { get; init; } = DisplayType.Week;
    public int HourFrom { get; init; } = 8;
    public int HourTo { get; init; } = 16;
    public bool HourFormat24 { get; set; } = true;

    internal DayOfWeek FirstDayOfWeek => SupportedDays.First();
    
    internal void Validate()
    {
        if (SupportedMonths.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(SupportedMonths)}.");
        if (SupportedDays.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(SupportedDays)}.");
        if (HourFrom < 0)
            throw new InvalidSetupException($"{nameof(HourFrom)} must be >= 0.");
        if (HourTo > 23)
            throw new InvalidSetupException($"{nameof(HourTo)} must be <= 23.");
        if (HourTo < HourFrom)
            throw new InvalidSetupException($"{nameof(HourTo)} must be greater than or equal to HourFrom.");
        if (!SupportedDays.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(SupportedDays)} required.");
        if (!SupportedDisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(DisplayType)} in {nameof(SupportedDisplayTypes)} required.");
        if (SupportedDisplayTypes.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate {nameof(DisplayType)} found in {nameof(SupportedDisplayTypes)}.");
        if (!SupportedDisplayTypes.Contains(DefaultDisplayType))
            throw new InvalidSetupException($"{nameof(DisplayType)} must be part of {nameof(SupportedDisplayTypes)}.");
    }
}