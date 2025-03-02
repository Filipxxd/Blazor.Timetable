using Timetable.Enums;
using Timetable.Exceptions;

namespace Timetable.Configuration;

public sealed class TimetableConfig
{
    /// <summary>
    /// Months shown in the timetable. Order has no impact.
    /// </summary>
    public IEnumerable<Month> Months { get; init; } =     [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June, 
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];
    
    /// <summary>
    /// Days shown in the timetable. Order has no impact.
    /// </summary>
    public IEnumerable<DayOfWeek> Days { get; init; } =     [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];
    
    /// <summary>
    /// Allowed display modes for the timetable.
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
    /// Use 24-hour format for display times. Defaults to true.
    /// </summary>
    public bool Is24HourFormat { get; init; } = true;
    
    /// <summary>
    /// First day of the week to display. Defaults to <see cref="DayOfWeek.Monday"/>. Must be part of <see cref="Days"/>.
    /// </summary>
    public DayOfWeek FirstDayOfWeek { get; init; } = DayOfWeek.Monday;
    
    /// <summary>
    /// First month of the year to display. Defaults to <see cref="Month.January"/>. Must be part of <see cref="Months"/>.
    /// </summary>
    public Month FirstMonthOfYear { get; init; } = Month.January;

    /// <summary>
    /// Display type of the timetable. Defaults to <see cref="DisplayType.Week"/>.
    /// </summary>
    public DisplayType DisplayType { get; set; } = DisplayType.Week;
    
    /// <summary>
    /// Current date of the timetable. Defaults to <see cref="DateTime.Today"/>.
    /// </summary>
    public DateTime CurrentDate { get; set; } = DateTime.Today;
    
    internal IEnumerable<int> Hours => Enumerable.Range(TimeFrom.Hour, TimeTo.Hour - TimeFrom.Hour + 1);
    
    internal void Validate()
    {
        if (TimeTo <= TimeFrom)
            throw new InvalidSetupException($"{nameof(TimeTo)} must be greater than ${nameof(TimeFrom)}.");
            
        if (TimeFrom.Minute % 15 != 0)
            throw new InvalidOperationException($"{nameof(TimeFrom)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");
        
        if (TimeTo.Minute % 15 != 0)
            throw new InvalidOperationException($"{nameof(TimeTo)} must be a quarter-hour interval (0, 15, 30, 45 minutes).");
        
        if (!DisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(Enums.DisplayType)} in {nameof(DisplayTypes)} required.");
        
        if (DisplayTypes.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate {nameof(Enums.DisplayType)} found in {nameof(DisplayTypes)}.");
        
        if (!DisplayTypes.Contains(DisplayType))
            throw new InvalidSetupException($"{nameof(Enums.DisplayType)} must be part of {nameof(DisplayTypes)}.");
        
        if (!Months.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");
        
        if (!Months.Contains(FirstMonthOfYear))
            throw new InvalidSetupException($"{nameof(FirstMonthOfYear)} must be part of {nameof(Months)}.");
        
        if (Months.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(Months)}.");
        
        if (!Days.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(Days)} required.");
        
        if (!Days.Contains(FirstDayOfWeek))
            throw new InvalidSetupException($"{nameof(FirstDayOfWeek)} must be part of {nameof(Days)}.");
        
        if (Days.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(Days)}.");
    }
}