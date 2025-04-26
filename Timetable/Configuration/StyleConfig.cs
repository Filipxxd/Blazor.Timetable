using System.Globalization;
using Timetable.Common.Exceptions;

namespace Timetable.Configuration;

public sealed class StyleConfig
{
    /// <summary>
    /// The color of the header event background. Must be a valid hex color.
    /// </summary>
    public string HeaderEventBgColor { get; set; } = "#FF9966";

    /// <summary>
    /// The color of the regular event background. Must be a valid hex color.
    /// </summary>
    public string RegularEventBgColor { get; set; } = "#67B6FF";

    internal void Validate()
    {
        if (!IsValidHex(HeaderEventBgColor))
            throw new InvalidSetupException($"Invalid hex color in {nameof(HeaderEventBgColor)} : {HeaderEventBgColor}");

        if (!IsValidHex(RegularEventBgColor))
            throw new InvalidSetupException($"Invalid hex color in {nameof(RegularEventBgColor)} : {RegularEventBgColor}");
    }

    private static bool IsValidHex(string color)
    {
        if (int.TryParse(color[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var _))
            return true;

        return false;
    }
}
