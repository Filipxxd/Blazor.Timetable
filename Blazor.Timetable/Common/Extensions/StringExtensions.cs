namespace Blazor.Timetable.Common.Extensions;

internal static class StringExtensions
{
    internal static string CapitalizeWords(this string input) =>
        string.Join(" ", input.Split(' ').Select(word => word.Capitalize()));

    internal static string Capitalize(this string input) =>
        input switch
        {
            "" => string.Empty,
            _ => input.Length == 1
                ? input.ToUpper()
                : input[0].ToString().ToUpper() + input[1..]
        };
}