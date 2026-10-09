using System.Globalization;

namespace FlagsRally.Helpers;

public static class DistanceFormatter
{
    /// <summary>
    /// Formats a distance for people: metres under 1 km (rounded to 10 m), otherwise kilometres.
    /// </summary>
    public static string Format(double kilometers)
    {
        if (kilometers < 1)
        {
            var meters = Math.Max(10, Math.Round(kilometers * 100) * 10);
            return string.Create(CultureInfo.InvariantCulture, $"{meters:0} m");
        }

        return kilometers < 10
            ? string.Create(CultureInfo.InvariantCulture, $"{kilometers:0.0} km")
            : string.Create(CultureInfo.InvariantCulture, $"{kilometers:0} km");
    }
}
