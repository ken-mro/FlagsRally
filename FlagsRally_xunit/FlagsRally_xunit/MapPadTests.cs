using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapPadTests
{
    [Fact]
    public void A_finger_starts_moving_the_map_inside_the_circle_and_zooming_on_the_ring()
    {
        Assert.Equal(MapPad.Zone.Pan, MapPad.ZoneAt(10, null));
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.RingStart + 1, null));
    }

    [Fact]
    public void The_edge_of_the_ring_does_not_flicker()
    {
        // Out past the ring's start: zooming. A little way back in: still zooming.
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.RingStart + 1, MapPad.Zone.Pan));
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.RingStart - 5, MapPad.Zone.Zoom));
        // Well inside again: moving the map.
        Assert.Equal(MapPad.Zone.Pan, MapPad.ZoneAt(MapPad.RingLeave - 1, MapPad.Zone.Zoom));
        // A finger past the pad's edge keeps zooming.
        Assert.Equal(MapPad.Zone.Zoom, MapPad.ZoneAt(MapPad.Radius * 3, MapPad.Zone.Zoom));
    }

    [Fact]
    public void The_map_moves_the_way_the_finger_leans_faster_further_out()
    {
        Assert.Equal((0, 0), MapPad.PanVelocity(2, -3));

        var (slowX, slowY) = MapPad.PanVelocity(12, 0);
        var (fastX, fastY) = MapPad.PanVelocity(30, 0);
        Assert.True(slowX > 0 && fastX > slowX);
        Assert.Equal(0, slowY);
        Assert.Equal(0, fastY);

        var (upX, upY) = MapPad.PanVelocity(0, -MapPad.RingStart);
        Assert.Equal(0, upX, 6);
        Assert.Equal(-MapPad.MaxPanSpeed, upY, 6);

        // Never faster than the top speed.
        var (_, far) = MapPad.PanVelocity(0, 500);
        Assert.Equal(MapPad.MaxPanSpeed, far, 6);
    }

    [Fact]
    public void Clockwise_round_the_ring_zooms_in_and_the_other_way_out()
    {
        // From the top (12 o'clock) to the right (3 o'clock) is clockwise on screen.
        var top = MapPad.Angle(0, -50);
        var right = MapPad.Angle(50, 0);
        Assert.Equal(Math.PI / 2 / MapPad.RadiansPerLevel, MapPad.ZoomChange(top, right), 6);
        Assert.Equal(-Math.PI / 2 / MapPad.RadiansPerLevel, MapPad.ZoomChange(right, top), 6);
    }

    [Fact]
    public void Passing_nine_oclock_does_not_jump()
    {
        // The angle wraps from +180 to −180 degrees at the left; the turn is still a small one.
        var justAbove = MapPad.Angle(-50, -1);
        var justBelow = MapPad.Angle(-50, 1);
        Assert.True(Math.Abs(MapPad.ZoomChange(justAbove, justBelow)) < 0.1);
        Assert.True(MapPad.ZoomChange(justBelow, justAbove) > 0);   // 8 to 10 o'clock is clockwise
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
