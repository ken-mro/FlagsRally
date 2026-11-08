namespace FlagsRally.Helpers;

/// <summary>
/// How the map's zoom slider turns a finger's movement into zoom levels.
/// </summary>
public static class MapZoom
{
    public const double MinZoom = 2;
    public const double MaxZoom = 21;

    /// <summary>A finger moving this far (in device-independent units) changes the zoom by one level.</summary>
    public const double UnitsPerLevel = 40;

    /// <summary>
    /// The zoom while the slider is dragged: up (negative <paramref name="totalY"/>) zooms in,
    /// down zooms out, from the zoom the drag started at.
    /// </summary>
    public static double ZoomFor(double startZoom, double totalY) =>
        Clamp(startZoom - totalY / UnitsPerLevel);

    /// <summary>One step in or out from the current zoom, as the + and − buttons did.</summary>
    public static double Step(double zoom, int step) => Clamp(zoom + step);

    /// <summary>Where the knob sits: with the finger, but kept inside the track.</summary>
    public static double KnobOffset(double totalY, double limit) =>
        limit <= 0 ? 0 : Math.Clamp(totalY, -limit, limit);

    static double Clamp(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);
}
