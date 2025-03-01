using School_Timetable.Enums;
using School_Timetable.Exceptions;

namespace School_Timetable.Configuration;

public sealed class TimetableConfig
{
    public IEnumerable<Month> Months { get; init; } =     [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June, 
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];
    public IEnumerable<DayOfWeek> Days { get; init; } =     [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];
    public IEnumerable<DisplayType> DisplayTypes { get; init; } = [DisplayType.Day, DisplayType.Week, DisplayType.Month]; 
    public DisplayType DisplayType { get; set; } = DisplayType.Week;
    public TimeOnly TimeFrom { get; init; } = new(0, 0);
    public TimeOnly TimeTo { get; init; } = new(23, 0);
    public bool Is24HourFormat { get; init; } = true;

    public DateTime CurrentDate { get; set; } = DateTime.Today;
    
    internal IEnumerable<int> Hours => Enumerable.Range(TimeFrom.Hour, TimeTo.Hour - TimeFrom.Hour + 1);
    
    internal void Validate()
    {
        if (Months.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(Months)}.");
        
        if (Days.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(Days)}.");
        
        if (TimeTo < TimeFrom)
            throw new InvalidSetupException($"{nameof(TimeTo)} must be greater than or equal to ${nameof(TimeFrom)}.");
        
        if (!Days.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");
        
        if (!DisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(Enums.DisplayType)} in {nameof(DisplayTypes)} required.");
        
        if (DisplayTypes.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate {nameof(Enums.DisplayType)} found in {nameof(DisplayTypes)}.");
        
        if (!DisplayTypes.Contains(DisplayType))
            throw new InvalidSetupException($"{nameof(Enums.DisplayType)} must be part of {nameof(DisplayTypes)}.");
    }
}