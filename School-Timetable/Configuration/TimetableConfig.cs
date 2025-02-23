using School_Timetable.Enums;
using School_Timetable.Exceptions;

namespace School_Timetable.Configuration;

public sealed class TimetableConfig
{
    public IEnumerable<Month> SelectedMonths { get; init; } =     [
        Month.January, Month.February, Month.March, Month.April, Month.May, Month.June, 
        Month.July, Month.August, Month.September, Month.October, Month.November, Month.December
    ];// TODO: Ensure realistic 
    public IEnumerable<DayOfWeek> SelectedDays { get; init; } =     [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ]; // TODO: Ensure realistic eg. wednesday after tuesday etc.
    public IEnumerable<DisplayType> SelectedDisplayTypes { get; init; } = [DisplayType.Day, DisplayType.Week, DisplayType.Month]; 
    public DisplayType DefaultDisplayType { get; init; } = DisplayType.Week;
    public TimeOnly TimeFrom { get; init; } = new(0, 0);
    public TimeOnly TimeTo { get; init; } = new(23, 0);
    public bool Is24HourFormat { get; init; } = true;

    internal IEnumerable<int> Hours => Enumerable.Range(TimeFrom.Hour, TimeTo.Hour - TimeFrom.Hour + 1);
    
    internal void Validate()
    {
        if (SelectedMonths.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate months found in {nameof(SelectedMonths)}.");
        
        if (SelectedDays.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate days found in {nameof(SelectedDays)}.");
        
        if (TimeTo < TimeFrom)
            throw new InvalidSetupException($"{nameof(TimeTo)} must be greater than or equal to ${nameof(TimeFrom)}.");
        
        if (!SelectedDays.Any())
            throw new InvalidSetupException($"At least one {nameof(DayOfWeek)} in {nameof(SelectedDays)} required.");
        
        if (!SelectedDisplayTypes.Any())
            throw new InvalidSetupException($"At least one {nameof(DisplayType)} in {nameof(SelectedDisplayTypes)} required.");
        
        if (SelectedDisplayTypes.GroupBy(x => x).Any(x => x.Count() > 1))
            throw new InvalidSetupException($"Duplicate {nameof(DisplayType)} found in {nameof(SelectedDisplayTypes)}.");
        
        if (!SelectedDisplayTypes.Contains(DefaultDisplayType))
            throw new InvalidSetupException($"{nameof(DisplayType)} must be part of {nameof(SelectedDisplayTypes)}.");
    }
}