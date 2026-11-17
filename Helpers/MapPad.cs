namespace FlagsRally.Helpers;

/// <summary>
/// How the map's thumb controller turns a finger's position into camera moves. A finger that slides
/// from the controller's button to one of two places opens a pad there: to the left for the move pad,
/// up for the zoom pad. The pad opens with its middle under the finger, and both pads are joysticks:
/// the further the finger is from the middle, the faster. The move pad moves the map the way the
/// finger leans. The zoom pad zooms in for a finger above its middle and out for one below.
/// All lengths are device-independent units.
/// </summary>
public static class MapPad
{
    public enum Mode { Move, Zoom }

    /// <summary>How far to the left of the button's centre the finger goes to open the move pad.</summary>
    public const double MoveSwitch = 70;

    /// <summary>How far above the button's centre the finger goes to open the zoom pad.</summary>
    public const double ZoomSwitch = 50;

    /// <summary>
    /// The pad opened by a finger at (<paramref name="x"/>, <paramref name="y"/>) from the button's
    /// centre: none until it is <see cref="MoveSwitch"/> to the left (the move pad) or
    /// <see cref="ZoomSwitch"/> up (the zoom pad); for a finger past both, the one it is further past.
    /// </summary>
    public static Mode? ModeAt(double x, double y)
    {
        var left = -x / MoveSwitch;
        var up = -y / ZoomSwitch;
        if (left < 1 && up < 1) return null;
        return left >= up ? Mode.Move : Mode.Zoom;
    }

    /// <summary>
    /// Whether a touch at (<paramref name="x"/>, <paramref name="y"/>) from an open pad's middle is on
    /// that pad, allowing <paramref name="slop"/> round it for a thumb.
    /// </summary>
    public static bool IsOnPad(Mode mode, double x, double y, double slop) => mode == Mode.Move
        ? Math.Sqrt(x * x + y * y) <= Radius + slop
        : Math.Abs(x) <= ZoomHalfWidth + slop && Math.Abs(y) <= ZoomHalfHeight + slop;

    /// <summary>The move pad's radius.</summary>
    public const double Radius = 64;

    /// <summary>
    /// The hole in the middle of the move pad, where a finger moves nothing: the slide that opened the
    /// pad, and a thumb that has not settled, stay inside it.
    /// </summary>
    public const double PanDeadZone = 20;

    /// <summary>The map's speed with the finger at the move pad's edge, in units per second.</summary>
    public const double PanSpeedAtEdge = 1100;

    /// <summary>
    /// How far past the pad's edge the speed keeps rising, as a part of the way from the dead zone to
    /// the edge: at 1.5 the finger can go half as far again, for 2.25 times the speed at the edge.
    /// </summary>
    public const double PanReach = 1.5;

    /// <summary>Half the zoom pad's height: its ＋ end is this far above the middle, its − end below.</summary>
    public const double ZoomHalfHeight = 80;

    /// <summary>Half the zoom pad's width.</summary>
    public const double ZoomHalfWidth = 28;

    /// <summary>A finger this close to the zoom pad's middle zooms nothing.</summary>
    public const double ZoomDeadZone = 8;

    /// <summary>How far above or below the middle gives <see cref="MaxZoomSpeed"/>; it counts no further.</summary>
    public const double ZoomFullReach = 64;

    /// <summary>The fastest zoom, in levels per second.</summary>
    public const double MaxZoomSpeed = 4;

    /// <summary>
    /// The map's speed for a finger at (<paramref name="x"/>, <paramref name="y"/>) on the move pad:
    /// in the finger's direction, nothing in the hole, slow just outside it for small corrections,
    /// <see cref="PanSpeedAtEdge"/> at the pad's edge and still rising a little way past it.
    /// </summary>
    public static (double X, double Y) PanVelocity(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= PanDeadZone) return (0, 0);
        var lean = Math.Min(PanReach, (distance - PanDeadZone) / (Radius - PanDeadZone));
        var speed = PanSpeedAtEdge * lean * lean;
        return (x / distance * speed, y / distance * speed);
    }

    /// <summary>
    /// The zoom's speed in levels per second for a finger <paramref name="y"/> below the zoom pad's
    /// middle (negative is above): in for above, out for below, slow near the middle and rising to
    /// <see cref="MaxZoomSpeed"/>.
    /// </summary>
    public static double ZoomSpeed(double y)
    {
        var size = Math.Abs(y);
        if (size <= ZoomDeadZone) return 0;
        var part = Math.Min(1, (size - ZoomDeadZone) / (ZoomFullReach - ZoomDeadZone));
        return -Math.Sign(y) * MaxZoomSpeed * part * part;
    }

    /// <summary>Where the knob sits: with the finger, but kept inside the pad.</summary>
    public static (double X, double Y) KnobOffset(double x, double y, double limit)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (limit <= 0 || distance <= 0) return (0, 0);
        if (distance <= limit) return (x, y);
        return (x / distance * limit, y / distance * limit);
    }

    /// <summary>
    /// The camera's target after the map moved (<paramref name="dx"/>, <paramref name="dy"/>) units on
    /// screen (right and down) at <paramref name="zoom"/>, with the map turned by <paramref name="bearing"/> degrees.
    /// </summary>
    public static (double Latitude, double Longitude) MoveTarget(double latitude, double longitude, double zoom, double bearing, double dx, double dy)
    {
        // Screen right/down to map east/south, undoing the map's rotation.
        var turn = bearing * Math.PI / 180;
        var east = dx * Math.Cos(turn) - dy * Math.Sin(turn);
        var south = dx * Math.Sin(turn) + dy * Math.Cos(turn);

        // Web Mercator: the whole world is 256 units wide at zoom 0 and doubles with each level.
        var world = 256 * Math.Pow(2, zoom);
        var x = (longitude + 180) / 360 + east / world;
        var sin = Math.Sin(Math.Clamp(latitude, -MaxLatitude, MaxLatitude) * Math.PI / 180);
        var y = 0.5 - Math.Log((1 + sin) / (1 - sin)) / (4 * Math.PI) + south / world;

        x -= Math.Floor(x);   // round the world
        var newLatitude = 90 - 360 * Math.Atan(Math.Exp((y - 0.5) * 2 * Math.PI)) / Math.PI;
        return (Math.Clamp(newLatitude, -MaxLatitude, MaxLatitude), x * 360 - 180);
    }

    const double MaxLatitude = 85;
}
