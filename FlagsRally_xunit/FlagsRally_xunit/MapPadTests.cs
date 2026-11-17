using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapPadTests
{
    [Fact]
    public void The_hole_rests_the_inner_ring_zooms_and_the_outer_ring_moves_the_map()
    {
        Assert.Equal(MapPad.Zone.Rest, MapPad.ZoneAt(0, null));
        Assert.Equal(MapPad.Zone.Rest, MapPad.ZoneAt(MapPad.HoleRadius - 1, MapPad.Zone.Rest));
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.HoleRadius + 1, MapPad.Zone.Rest));
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.PanStart - 1, MapPad.Zone.Zoom));
        Assert.Equal(MapPad.Zone.Pan, MapPad.ZoneAt(MapPad.PanStart + 1, MapPad.Zone.Zoom));
    }

    [Fact]
    public void The_edges_of_the_rings_do_not_flicker()
    {
        // Just back inside the outer ring: still moving the map. Well inside: zooming.
        Assert.Equal(MapPad.Zone.Pan, MapPad.ZoneAt(MapPad.PanStart - 3, MapPad.Zone.Pan));
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.PanLeave - 1, MapPad.Zone.Pan));
        // Just back inside the hole's edge: still zooming. Well inside: resting.
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.HoleRadius - 2, MapPad.Zone.Zoom));
        Assert.Equal(MapPad.Zone.Rest, MapPad.ZoneAt(MapPad.HoleReturn - 1, MapPad.Zone.Zoom));
        // A finger past the pad's edge keeps moving the map.
        Assert.Equal(MapPad.Zone.Pan, MapPad.ZoneAt(MapPad.Radius * 3, MapPad.Zone.Pan));
        // A fast finger can go from the outer ring to the hole in one move.
        Assert.Equal(MapPad.Zone.Rest, MapPad.ZoneAt(2, MapPad.Zone.Pan));
    }

    [Fact]
    public void The_map_moves_the_way_the_finger_leans_faster_further_out()
    {
        // Nothing in the hole or on the inner ring.
        Assert.Equal((0, 0), MapPad.PanVelocity(2, -3));
        Assert.Equal((0, 0), MapPad.PanVelocity(MapPad.PanStart - 1, 0));

        var (slowX, slowY) = MapPad.PanVelocity(MapPad.PanStart + 6, 0);
        var (fastX, fastY) = MapPad.PanVelocity(MapPad.PanStart + 20, 0);
        Assert.True(slowX > 0 && fastX > slowX);
        Assert.Equal(0, slowY);
        Assert.Equal(0, fastY);

        var (upX, upY) = MapPad.PanVelocity(0, -MapPad.Radius);
        Assert.Equal(0, upX, 6);
        Assert.Equal(-MapPad.PanSpeedAtEdge, upY, 6);

        // Faster still past the pad's edge, up to a top speed.
        var (_, past) = MapPad.PanVelocity(0, MapPad.Radius + 8);
        var (_, far) = MapPad.PanVelocity(0, 500);
        Assert.True(past > MapPad.PanSpeedAtEdge && far > past);
        Assert.Equal(MapPad.PanSpeedAtEdge * MapPad.PanReach * MapPad.PanReach, far, 6);
    }

    [Fact]
    public void Clockwise_from_where_the_finger_came_on_zooms_in_and_the_other_way_out()
    {
        var top = MapPad.Angle(0, -50);
        var right = MapPad.Angle(50, 0);
        Assert.True(MapPad.ZoomSpeed(MapPad.Slide(0, top, right)) > 0);
        Assert.True(MapPad.ZoomSpeed(MapPad.Slide(0, right, top)) < 0);
        Assert.Equal(-MapPad.ZoomSpeed(Math.PI / 2), MapPad.ZoomSpeed(-Math.PI / 2), 9);
    }

    [Fact]
    public void The_zoom_is_faster_the_further_round_the_finger_slides()
    {
        Assert.Equal(0, MapPad.ZoomSpeed(0));
        Assert.Equal(0, MapPad.ZoomSpeed(MapPad.ZoomDeadAngle));   // a resting thumb does not zoom
        var little = MapPad.ZoomSpeed(Math.PI / 6);
        var more = MapPad.ZoomSpeed(Math.PI / 3);
        Assert.True(0 < little && little < more && more < MapPad.MaxZoomSpeed);
        Assert.Equal(MapPad.MaxZoomSpeed, MapPad.ZoomSpeed(MapPad.ZoomFullAngle), 9);
        Assert.Equal(MapPad.MaxZoomSpeed, MapPad.ZoomSpeed(Math.PI), 9);
    }

    [Fact]
    public void Passing_nine_oclock_does_not_jump()
    {
        var justAbove = MapPad.Angle(-50, -1);
        var justBelow = MapPad.Angle(-50, 1);
        Assert.True(Math.Abs(MapPad.Turn(justAbove, justBelow)) < 0.1);
        Assert.True(MapPad.Turn(justBelow, justAbove) > 0);   // 8 to 10 o'clock is clockwise
    }

    [Fact]
    public void A_finger_that_goes_most_of_the_way_round_keeps_zooming_the_same_way()
    {
        // Clockwise in steps of 30 degrees, 300 degrees in all: still zooming in, at full speed.
        var slide = 0.0;
        for (var degrees = 0; degrees < 300; degrees += 30)
        {
            slide = MapPad.Slide(slide, AngleAt(degrees), AngleAt(degrees + 30));
        }
        Assert.Equal(MapPad.ZoomFullAngle, slide, 9);
        Assert.Equal(MapPad.MaxZoomSpeed, MapPad.ZoomSpeed(slide), 9);
    }

    // A finger's angle as the pad sees it (between −180 and 180 degrees) after going this far round.
    static double AngleAt(int degrees)
    {
        var radians = degrees * Math.PI / 180;
        return MapPad.Angle(Math.Cos(radians), Math.Sin(radians));
    }

    [Theory]
    [InlineData(10, 0, 49, 10, 0)]
    [InlineData(100, 0, 49, 49, 0)]
    [InlineData(0, -200, 49, 0, -49)]
    [InlineData(0, 0, 49, 0, 0)]
    public void The_knob_follows_the_finger_inside_the_pad(double x, double y, double limit, double knobX, double knobY)
    {
        var (offsetX, offsetY) = MapPad.KnobOffset(x, y, limit);
        Assert.Equal(knobX, offsetX, 6);
        Assert.Equal(knobY, offsetY, 6);
    }

    [Fact]
    public void Moving_right_goes_east_and_down_goes_south()
    {
        var (latitude, longitude) = MapPad.MoveTarget(35, 139, 10, 0, 100, 0);
        Assert.Equal(35, latitude, 6);
        // 100 units of the 256 * 2^10 that make 360 degrees.
        Assert.Equal(139 + 100 * 360.0 / (256 * 1024), longitude, 6);

        (latitude, longitude) = MapPad.MoveTarget(35, 139, 10, 0, 0, 100);
        Assert.True(latitude < 35);
        Assert.Equal(139, longitude, 6);
    }

    [Fact]
    public void The_same_lean_covers_half_the_ground_one_level_further_in()
    {
        var (_, at10) = MapPad.MoveTarget(0, 0, 10, 0, 100, 0);
        var (_, at11) = MapPad.MoveTarget(0, 0, 11, 0, 100, 0);
        Assert.Equal(at10 / 2, at11, 9);
    }

    [Fact]
    public void A_turned_map_still_moves_the_way_the_finger_leans()
    {
        // With east at the top of the screen (bearing 90), leaning up goes east.
        var (latitude, longitude) = MapPad.MoveTarget(0, 0, 10, 90, 0, -100);
        Assert.Equal(0, latitude, 6);
        Assert.True(longitude > 0);
    }

    [Fact]
    public void The_map_goes_round_the_world_and_stops_short_of_the_poles()
    {
        var (_, longitude) = MapPad.MoveTarget(0, 179.9, 2, 0, 50, 0);
        Assert.InRange(longitude, -180, -150);

        var (latitude, _) = MapPad.MoveTarget(84, 0, 2, 0, 0, -5000);
        Assert.Equal(85, latitude, 6);
    }
}
