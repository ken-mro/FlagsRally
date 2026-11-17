namespace FlagsRally.Helpers;

/// <summary>
/// The map's zoom range, for the thumb pad's zoom and its ＋ and − steps.
/// </summary>
public static class MapZoom
{
    public const double MinZoom = 2;
    public const double MaxZoom = 21;

    /// <summary>One step in or out from the current zoom, from a tap on ＋ or −.</summary>
    public static double Step(double zoom, int step) => Clamp(zoom + step);

    public static double Clamp(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);
}
