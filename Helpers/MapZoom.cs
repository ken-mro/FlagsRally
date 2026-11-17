namespace FlagsRally.Helpers;

/// <summary>
/// The map's zoom range the thumb pad's zoom stays in.
/// </summary>
public static class MapZoom
{
    public const double MinZoom = 2;
    public const double MaxZoom = 21;

    public static double Clamp(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);
}
