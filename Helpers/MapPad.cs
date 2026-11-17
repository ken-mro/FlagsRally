namespace FlagsRally.Helpers;

/// <summary>
/// How the map's thumb pad turns a finger's position into camera moves. The pad is two rings round
/// a hole where the thumb rests. The inner ring is a joystick for the zoom: from where the finger
/// came onto it, a slide clockwise zooms in and one anticlockwise zooms out, faster the further round.
/// The outer ring is a joystick for the map: it keeps moving the way the finger leans, faster the
/// further out, also past the pad's edge.
/// All lengths are device-independent units measured from the pad's centre.
/// </summary>
public static class MapPad
{
    public enum Zone { Rest, Zoom, Pan }

    /// <summary>The pad's radius, to the outside of the outer ring.</summary>
    public const double Radius = 64;

    /// <summary>The hole in the middle, where a finger changes nothing. The inner ring starts here.</summary>
    public const double HoleRadius = 14;

    /// <summary>How far back in the finger has to come to leave the inner ring, so the edge does not flicker.</summary>
    public const double HoleReturn = 10;

    /// <summary>Where the inner ring ends and the outer ring starts.</summary>
    public const double PanStart = 38;

    /// <summary>How far back in the finger has to come to leave the outer ring.</summary>
    public const double PanLeave = 31;

    /// <summary>The map's speed with the finger at the pad's edge, in units per second.</summary>
    public const double PanSpeedAtEdge = 1100;

    /// <summary>
    /// How far past the pad's edge the speed keeps rising, as a part of the outer ring's width: at 1.5
    /// the finger can go half a ring's width past the edge, for 2.25 times the speed at the edge.
    /// </summary>
    public const double PanReach = 1.5;

    /// <summary>A finger this close (in radians) to where it came onto the inner ring zooms nothing.</summary>
    public const double ZoomDeadAngle = Math.PI / 22.5;   // 8 degrees

    /// <summary>The slide round the inner ring that gives <see cref="MaxZoomSpeed"/>; it counts no further.</summary>
    public const double ZoomFullAngle = Math.PI * 2 / 3;   // 120 degrees

    /// <summary>The fastest zoom, in levels per second.</summary>
    public const double MaxZoomSpeed = 4;

    /// <summary>The zone a finger is in, given the zone it was in (null when it has just gone down).</summary>
    public static Zone ZoneAt(double distance, Zone? current) => current switch
    {
        Zone.Pan => distance >= PanLeave ? Zone.Pan : distance >= HoleReturn ? Zone.Zoom : Zone.Rest,
        Zone.Zoom => distance > PanStart ? Zone.Pan : distance >= HoleReturn ? Zone.Zoom : Zone.Rest,
        _ => distance > PanStart ? Zone.Pan : distance > HoleRadius ? Zone.Zoom : Zone.Rest,
    };

    /// <summary>
    /// The map's speed for a finger on the outer ring at (<paramref name="x"/>, <paramref name="y"/>):
    /// in the finger's direction, slow where the ring starts for small corrections,
    /// <see cref="PanSpeedAtEdge"/> at the pad's edge and still rising a little way past it.
    /// </summary>
    public static (double X, double Y) PanVelocity(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= PanStart) return (0, 0);
        var lean = Math.Min(PanReach, (distance - PanStart) / (Radius - PanStart));
        var speed = PanSpeedAtEdge * lean * lean;
        return (x / distance * speed, y / distance * speed);
    }

    /// <summary>The finger's angle round the pad; it grows clockwise on screen (y points down).</summary>
    public static double Angle(double x, double y) => Math.Atan2(y, x);

    /// <summary>How far a finger turned from one angle to another, the short way round; clockwise is positive.</summary>
    public static double Turn(double fromAngle, double toAngle)
    {
        var turned = toAngle - fromAngle;
        if (turned > Math.PI) turned -= 2 * Math.PI;
        if (turned <= -Math.PI) turned += 2 * Math.PI;
        return turned;
    }

    /// <summary>
    /// The slide after one more move of the finger, kept to <see cref="ZoomFullAngle"/> either way so a
    /// finger that goes most of the way round does not come out on the other side.
    /// </summary>
    public static double Slide(double slide, double fromAngle, double toAngle) =>
        Math.Clamp(slide + Turn(fromAngle, toAngle), -ZoomFullAngle, ZoomFullAngle);

    /// <summary>
    /// The zoom's speed in levels per second for a finger that has slid <paramref name="slide"/> radians
    /// round the ring from where it came on: in for clockwise, slow near the start and rising to
    /// <see cref="MaxZoomSpeed"/>.
    /// </summary>
    public static double ZoomSpeed(double slide)
    {
        var size = Math.Abs(slide);
        if (size <= ZoomDeadAngle) return 0;
        var part = Math.Min(1, (size - ZoomDeadAngle) / (ZoomFullAngle - ZoomDeadAngle));
        return Math.Sign(slide) * MaxZoomSpeed * part * part;
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
