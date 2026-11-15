using Maui.GoogleMaps;

namespace FlagsRally.Helpers;

/// <summary>Between the map's positions (Maui.GoogleMaps) and the device's locations (MAUI).</summary>
public static class GeoExtensions
{
    public static Location ToLocation(this Position position) => new(position.Latitude, position.Longitude);

    public static Position ToPosition(this Location location) => new(location.Latitude, location.Longitude);
}
