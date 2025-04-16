using System.Globalization;

namespace Timetable.Configuration;

internal static class CultureConfig
{
    public static CultureInfo CultureInfo { get; private set; } = CultureInfo.InvariantCulture;

    public static void SetCulture(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
    }
}
