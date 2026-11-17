using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapPadTests
{
    [Theory]
    [InlineData(-20, 0, null)]                 // not far enough yet
    [InlineData(0, -40, null)]
    [InlineData(-60, -45, null)]               // nor on the way to either
    [InlineData(60, 0, null)]                  // right and down open nothing
    [InlineData(0, 80, null)]
    [InlineData(-70, 0, MapPad.Mode.Move)]     // at the place to the left
    [InlineData(-75, -20, MapPad.Mode.Move)]
    [InlineData(0, -50, MapPad.Mode.Zoom)]     // at the place above
    [InlineData(-20, -55, MapPad.Mode.Zoom)]
    [InlineData(-90, -55, MapPad.Mode.Move)]   // past both: the one it is further past
    [InlineData(-72, -70, MapPad.Mode.Zoom)]
    public void A_finger_that_reaches_the_place_to_the_left_or_above_opens_that_pad(double x, double y, MapPad.Mode? mode)
    {
        Assert.Equal(mode, MapPad.ModeAt(x, y));
    }

    [Theory]
    [InlineData(MapPad.Mode.Move, 0, 0, true)]
    [InlineData(MapPad.Mode.Move, -60, 20, true)]
    [InlineData(MapPad.Mode.Move, 68, 0, true)]      // just off the ring: still the thumb's
    [InlineData(MapPad.Mode.Move, 60, 60, false)]    // past its corner
    [InlineData(MapPad.Mode.Move, 0, -90, false)]
    [InlineData(MapPad.Mode.Zoom, 0, -78, true)]
    [InlineData(MapPad.Mode.Zoom, 30, 70, true)]
    [InlineData(MapPad.Mode.Zoom, 60, 0, false)]     // beside the bar
    [InlineData(MapPad.Mode.Zoom, 0, 100, false)]    // under it
    public void A_touch_is_on_the_open_pad_or_outside_it(MapPad.Mode mode, double x, double y, bool onPad)
    {
        Assert.Equal(onPad, MapPad.IsOnPad(mode, x, y, slop: 6));
    }

    [Fact]
    public void The_map_moves_the_way_the_finger_leans_faster_further_out()
    {
        // A thumb in the hole moves nothing, also right at its edge.
        Assert.Equal((0, 0), MapPad.PanVelocity(2, -3));
        Assert.Equal((0, 0), MapPad.PanVelocity(-MapPad.PanDeadZone, 0));

        var (slowX, slowY) = MapPad.PanVelocity(30, 0);
        var (fastX, fastY) = MapPad.PanVelocity(50, 0);
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
    public void A_finger_above_the_middle_zooms_in_and_one_below_zooms_out()
    {
        Assert.True(MapPad.ZoomSpeed(-30) > 0);
        Assert.True(MapPad.ZoomSpeed(30) < 0);
        Assert.Equal(-MapPad.ZoomSpeed(-30), MapPad.ZoomSpeed(30), 9);
    }

    [Fact]
    public void The_zoom_is_faster_the_further_the_finger_is_from_the_middle()
    {
        Assert.Equal(0, MapPad.ZoomSpeed(0));
        Assert.Equal(0, MapPad.ZoomSpeed(-MapPad.ZoomDeadZone));   // a resting thumb does not zoom
        var little = MapPad.ZoomSpeed(-20);
        var more = MapPad.ZoomSpeed(-45);
        Assert.True(0 < little && little < more && more < MapPad.MaxZoomSpeed);
        Assert.Equal(MapPad.MaxZoomSpeed, MapPad.ZoomSpeed(-MapPad.ZoomFullReach), 9);
        Assert.Equal(MapPad.MaxZoomSpeed, MapPad.ZoomSpeed(-500), 9);
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
