namespace FlagsRally.Helpers;

/// <summary>
/// How the map's ring controller (<c>MapRingController</c>) turns a thumb into camera moves.
/// The controller is two circles with one centre: a ring round the outside that zooms, and a base in
/// the middle, about half the ring's width, with a knob on it that moves the map.
/// <list type="bullet">
/// <item>The knob is a joystick: the map moves the way it leans, faster the further it leans.</item>
/// <item>The ring is a shuttle: where the thumb comes down is the reference point, and the angle it has
/// slid round from there sets the zoom's speed, clockwise in and anticlockwise out.</item>
/// </list>
/// Angles are in degrees on screen, measured clockwise from the right (y points down).
/// All lengths are device-independent units.
/// </summary>
public static class MapRing
{
    public enum Part { Joystick, Ring, Outside }

    /// <summary>The ring's radius.</summary>
    public const double OuterRadius = 84;

    /// <summary>The base's radius, about half the ring's; the edge between the two parts.</summary>
    public const double BaseRadius = 42;

    /// <summary>The knob's radius, a size smaller than the base it stays inside.</summary>
    public const double KnobRadius = 27;

    /// <summary>How far round the ring a touch still counts as on it.</summary>
    public const double RingSlop = 6;

    /// <summary>A knob leaned this little moves nothing, so a thumb that wobbles leaves the map still.</summary>
    public const double PanDeadZone = 5;

    /// <summary>The map's speed with the thumb at the base's edge, in units per second.</summary>
    public const double PanSpeedAtEdge = 1200;

    /// <summary>
    /// The power the lean is raised to: above 1 the speed rises slowly at first, for small
    /// corrections with a shallow lean, and quickly near the edge.
    /// </summary>
    public const double PanCurve = 2.2;

    /// <summary>A thumb this close to the reference point, either way round, zooms nothing.</summary>
    public const double ZoomDeadZoneDegrees = 5;

    /// <summary>How far round either way counts, a little under half the ring.</summary>
    public const double ZoomMaxDegrees = 165;

    /// <summary>The fastest zoom, in levels per second (one level doubles the scale).</summary>
    public const double MaxZoomSpeed = 6;

    /// <summary>The power the slide is raised to, like <see cref="PanCurve"/>; at 1 the speed is in step with the angle.</summary>
    public const double ZoomCurve = 1;

    /// <summary>How far the ring's ticks turn for each doubling of the scale.</summary>
    public const double TickDegreesPerLevel = 30;

    /// <summary>
    /// Which part a touch at (<paramref name="x"/>, <paramref name="y"/>) from the centre is on: the
    /// base and its knob, the ring, or neither.
    /// </summary>
    public static Part PartAt(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= BaseRadius) return Part.Joystick;
        return distance <= OuterRadius + RingSlop ? Part.Ring : Part.Outside;
    }

    /// <summary>
    /// The map's speed for a thumb at (<paramref name="x"/>, <paramref name="y"/>) from the centre:
    /// the way it leans, nothing in the dead zone, slow just past it and <see cref="PanSpeedAtEdge"/>
    /// at the base's edge, where it counts no further.
    /// </summary>
    public static (double X, double Y) PanVelocity(double x, double y)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (distance <= PanDeadZone) return (0, 0);
        var lean = Math.Min(1, (distance - PanDeadZone) / (BaseRadius - PanDeadZone));
        var speed = PanSpeedAtEdge * Math.Pow(lean, PanCurve);
        return (x / distance * speed, y / distance * speed);
    }

    /// <summary>Where the knob sits: with the thumb, but no further than <paramref name="limit"/> from the centre.</summary>
    public static (double X, double Y) KnobOffset(double x, double y, double limit)
    {
        var distance = Math.Sqrt(x * x + y * y);
        if (limit <= 0 || distance <= 0) return (0, 0);
        if (distance <= limit) return (x, y);
        return (x / distance * limit, y / distance * limit);
    }

    /// <summary>The angle of (<paramref name="x"/>, <paramref name="y"/>) from the centre, clockwise from the right.</summary>
    public static double AngleOf(double x, double y) => Math.Atan2(y, x) * 180 / Math.PI;

    /// <summary>
    /// How far the thumb has gone round from the reference point, after it moved from
    /// <paramref name="fromAngle"/> to <paramref name="toAngle"/>. The steps are added up, each the
    /// short way round, so going past the top or the bottom does not make the value jump, and the
    /// total stops at <see cref="ZoomMaxDegrees"/> either way. Positive is clockwise.
    /// </summary>
    public static double AddStep(double shift, double fromAngle, double toAngle)
    {
        var step = (toAngle - fromAngle) % 360;
        if (step > 180) step -= 360;
        if (step <= -180) step += 360;
        return Math.Clamp(shift + step, -ZoomMaxDegrees, ZoomMaxDegrees);
    }

    /// <summary>
    /// The zoom's speed in levels per second for a thumb <paramref name="shift"/> degrees round from
    /// the reference point: in for clockwise, out for anticlockwise, nothing in the dead zone, and
    /// faster the further round, up to <see cref="MaxZoomSpeed"/>.
    /// </summary>
    public static double ZoomSpeed(double shift)
    {
        var size = Math.Abs(shift);
        if (size <= ZoomDeadZoneDegrees) return 0;
        var part = Math.Min(1, (size - ZoomDeadZoneDegrees) / (ZoomMaxDegrees - ZoomDeadZoneDegrees));
        return Math.Sign(shift) * MaxZoomSpeed * Math.Pow(part, ZoomCurve);
    }

    /// <summary>
    /// How far the ring's ticks are turned at <paramref name="zoom"/>: in step with the logarithm of the
    /// scale, none at the starting zoom, and no further once the zoom is at its end.
    /// </summary>
    public static double TickRotation(double zoom, double homeZoom) => (MapZoom.Clamp(zoom) - homeZoom) * TickDegreesPerLevel;

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
