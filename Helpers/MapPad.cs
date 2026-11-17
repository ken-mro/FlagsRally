namespace FlagsRally.Helpers;

/// <summary>
/// How the map's thumb pad turns a finger's position into camera moves. Inside the inner circle the
/// pad is a joystick: the map keeps moving the way the finger leans, faster the further out.
/// The ring around it is a second joystick, for the zoom: from where the finger came onto the ring,
/// a slide clockwise zooms in and one anticlockwise zooms out, faster the further round it goes.
/// All lengths are device-independent units measured from the pad's centre.
/// </summary>
public static class MapPad
{
    public enum Zone { Pan, Zoom }

    /// <summary>The pad's radius, to the outside of the ring.</summary>
    public const double Radius = 64;

    /// <summary>Where the ring starts.</summary>
    public const double RingStart = 38;

    /// <summary>How far back in the finger has to come to leave the ring, so the edge does not flicker.</summary>
    public const double RingLeave = 28;

    /// <summary>A finger this close to the centre moves nothing.</summary>
    public const double DeadZone = 6;

    /// <summary>The map's speed with the finger at the inner circle's edge, in units per second.</summary>
    public const double MaxPanSpeed = 700;

    /// <summary>A finger this close (in radians) to where it came onto the ring zooms nothing.</summary>
    public const double ZoomDeadAngle = Math.PI / 22.5;   // 8 degrees

    /// <summary>The slide round the ring that gives <see cref="MaxZoomSpeed"/>; it counts no further.</summary>
    public const double ZoomFullAngle = Math.PI * 2 / 3;   // 120 degrees

    /// <summary>The fastest zoom, in levels per second.</summary>
    public const double MaxZoomSpeed = 4;

    /// <summary>The zone a finger is in, given the zone it was in (null when it has just gone down).</summary>
    public static Zone ZoneAt(double distance, Zone? current) => current switch
    {
        Zone.Zoom => distance < RingLeave ? Zone.Pan : Zone.Zoom,
        _ => distance > RingStart ? Zone.Zoom : Zone.Pan,
    };

    /// <summary>
    /// The map's speed for a finger at (<paramref name="x"/>, <paramref name="y"/>): in the finger's
    /// direction, slow near the centre for small corrections and rising to <see cref="MaxPanSpeed"/>.
    /// </summary>
    public static (double X, double Y) PanVelocity(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= DeadZone) return (0, 0);
        var lean = Math.Min(1, (distance - DeadZone) / (RingStart - DeadZone));
        var speed = MaxPanSpeed * lean * lean;
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
