namespace FlagsRally.Helpers;

/// <summary>
/// How the map's thumb pad turns a finger's position into camera moves. Inside the inner circle the
/// pad is a joystick: the map keeps moving the way the finger leans, faster the further out.
/// On the ring around it, sliding round the pad zooms: clockwise in, anticlockwise out, and a tap
/// on its right or left side zooms one step in or out.
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

    /// <summary>Sliding this far round the pad changes the zoom by one level.</summary>
    public const double RadiansPerLevel = Math.PI / 3;

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

    /// <summary>The zoom change for a finger that went from one angle to another, the short way round.</summary>
    public static double ZoomChange(double fromAngle, double toAngle)
    {
        var turned = toAngle - fromAngle;
        if (turned > Math.PI) turned -= 2 * Math.PI;
        if (turned <= -Math.PI) turned += 2 * Math.PI;
        return turned / RadiansPerLevel;
    }

    /// <summary>
    /// The zoom step for a tap at (<paramref name="x"/>, <paramref name="y"/>): +1 on the ring's
    /// right side, where ＋ is drawn, −1 on its left side (−), 0 anywhere else.
    /// </summary>
    public static int StepAt(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= RingStart || Math.Abs(x) <= Math.Abs(y)) return 0;
        return x > 0 ? 1 : -1;
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
