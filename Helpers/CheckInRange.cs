using System.Globalization;
using Maui.GoogleMaps;

namespace FlagsRally.Helpers;

/// <summary>
/// How far from where you are a check-in may be recorded: a place on a custom board, or the
/// check-in spot you drop and drag on the map, has to be within this distance of your location.
/// </summary>
public static class CheckInRange
{
    public const double LimitKm = 0.05;

    public static bool IsWithin(double distanceKm) => distanceKm <= LimitKm;

    /// <summary>How far a place on the map is from a location, in kilometres.</summary>
    public static double DistanceKm(Position place, Location from) =>
        place.ToLocation().CalculateDistance(from, DistanceUnits.Kilometers);

    public static string LimitText => DistanceFormatter.Format(LimitKm);

    /// <summary>
    /// The distance to the check-in spot to the metre, rounded up so a spot shown as within the limit really is.
    /// </summary>
    public static string FormatDistance(double distanceKm) => distanceKm < 1
        ? string.Create(CultureInfo.InvariantCulture, $"{Math.Ceiling(distanceKm * 1000):0} m")
        : DistanceFormatter.Format(distanceKm);
}
