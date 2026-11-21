using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapRingTests
{
    [Theory]
    [InlineData(0, 0, MapRing.Part.Joystick)]
    [InlineData(30, -25, MapRing.Part.Joystick)]
    [InlineData(MapRing.BaseRadius, 0, MapRing.Part.Joystick)]      // the base's edge is the base's
    [InlineData(0, MapRing.BaseRadius + 1, MapRing.Part.Ring)]
    [InlineData(-60, 50, MapRing.Part.Ring)]
    [InlineData(MapRing.OuterRadius + 4, 0, MapRing.Part.Ring)]     // just off the ring: still the thumb's
    [InlineData(80, 80, MapRing.Part.Outside)]                      // the square's corner
    public void Where_a_touch_starts_chooses_the_joystick_or_the_ring(double x, double y, MapRing.Part part)
    {
        Assert.Equal(part, MapRing.PartAt(x, y));
    }

    [Fact]
    public void The_map_moves_the_way_the_knob_leans_faster_further_out()
    {
        // A wobbling thumb moves nothing.
        Assert.Equal((0, 0), MapRing.PanVelocity(2, -3));
        Assert.Equal((0, 0), MapRing.PanVelocity(-MapRing.PanDeadZone, 0));

        var (slowX, slowY) = MapRing.PanVelocity(12, 0);
        var (fastX, _) = MapRing.PanVelocity(30, 0);
        Assert.True(slowX > 0 && fastX > slowX);
        Assert.Equal(0, slowY);

        var (upX, upY) = MapRing.PanVelocity(0, -MapRing.BaseRadius);
        Assert.Equal(0, upX, 6);
        Assert.Equal(-MapRing.PanSpeedAtEdge, upY, 6);

        // The base's edge is the top speed; a thumb past it goes no faster.
        var (_, far) = MapRing.PanVelocity(0, 300);
        Assert.Equal(MapRing.PanSpeedAtEdge, far, 6);
    }

    [Fact]
    public void A_shallow_lean_is_slower_than_its_share_of_the_top_speed()
    {
        var middle = MapRing.PanDeadZone + (MapRing.BaseRadius - MapRing.PanDeadZone) / 2;
        var (speed, _) = MapRing.PanVelocity(middle, 0);
        Assert.True(speed < MapRing.PanSpeedAtEdge / 4);
    }

    [Theory]
    [InlineData(5, 0, 15, 5, 0)]
    [InlineData(100, 0, 15, 15, 0)]
    [InlineData(0, -200, 15, 0, -15)]
    [InlineData(0, 0, 15, 0, 0)]
    public void The_knob_follows_the_thumb_inside_the_base(double x, double y, double limit, double knobX, double knobY)
    {
        var (offsetX, offsetY) = MapRing.KnobOffset(x, y, limit);
        Assert.Equal(knobX, offsetX, 6);
        Assert.Equal(knobY, offsetY, 6);
    }

    [Theory]
    [InlineData(0)]      // right
    [InlineData(90)]     // bottom
    [InlineData(178)]    // left, sliding across it
    [InlineData(-90)]    // top
    [InlineData(-178)]
    public void Clockwise_zooms_in_and_anticlockwise_out_wherever_the_thumb_starts(double start)
    {
        var clockwise = MapRing.AddStep(0, start, start + 40);
        var anticlockwise = MapRing.AddStep(0, start, start - 40);
        Assert.Equal(40, clockwise, 9);
        Assert.Equal(-40, anticlockwise, 9);
        Assert.True(MapRing.ZoomSpeed(clockwise) > 0);
        Assert.True(MapRing.ZoomSpeed(anticlockwise) < 0);
    }

    [Fact]
    public void Going_past_the_left_of_the_ring_does_not_make_the_shift_jump()
    {
        // atan2 goes from 180 to -180 at the left; the steps are added the short way round.
        var shift = MapRing.AddStep(0, 170, 179);
        shift = MapRing.AddStep(shift, 179, -175);
        shift = MapRing.AddStep(shift, -175, -170);
        Assert.Equal(20, shift, 9);

        shift = MapRing.AddStep(shift, -170, 175);
        Assert.Equal(5, shift, 9);
    }

    [Fact]
    public void The_shift_stops_a_little_under_half_way_round()
    {
        var shift = 0.0;
        for (var angle = 0; angle < 300; angle += 10) shift = MapRing.AddStep(shift, angle, angle + 10);
        Assert.Equal(MapRing.ZoomMaxDegrees, shift);

        // Coming back counts from the end, not from where the thumb went past it.
        shift = MapRing.AddStep(shift, 300, 290);
        Assert.Equal(MapRing.ZoomMaxDegrees - 10, shift, 9);

        shift = 0;
        for (var angle = 0; angle > -300; angle -= 10) shift = MapRing.AddStep(shift, angle, angle - 10);
        Assert.Equal(-MapRing.ZoomMaxDegrees, shift);
    }

    [Fact]
    public void Back_at_the_reference_point_the_zoom_stops()
    {
        var shift = MapRing.AddStep(0, 30, 90);
        Assert.True(MapRing.ZoomSpeed(shift) > 0);
        shift = MapRing.AddStep(shift, 90, 33);
        Assert.Equal(0, MapRing.ZoomSpeed(shift));
        Assert.Equal(0, MapRing.ZoomSpeed(-MapRing.ZoomDeadZoneDegrees));
    }

    [Fact]
    public void The_zoom_is_faster_the_further_round_the_thumb_is()
    {
        var little = MapRing.ZoomSpeed(30);
        var more = MapRing.ZoomSpeed(90);
        Assert.True(0 < little && little < more && more < MapRing.MaxZoomSpeed);
        Assert.Equal(MapRing.MaxZoomSpeed, MapRing.ZoomSpeed(MapRing.ZoomMaxDegrees), 9);
        Assert.Equal(-MapRing.ZoomSpeed(60), MapRing.ZoomSpeed(-60), 9);
    }

    [Fact]
    public void The_ticks_turn_with_the_log_of_the_scale_and_stop_at_the_ends()
    {
        Assert.Equal(0, MapRing.TickRotation(14, 14));
        Assert.Equal(MapRing.TickDegreesPerLevel, MapRing.TickRotation(15, 14), 9);
        Assert.Equal(-2 * MapRing.TickDegreesPerLevel, MapRing.TickRotation(12, 14), 9);
        Assert.Equal(MapRing.TickRotation(MapZoom.MaxZoom, 14), MapRing.TickRotation(MapZoom.MaxZoom + 3, 14));
        Assert.Equal(MapRing.TickRotation(MapZoom.MinZoom, 14), MapRing.TickRotation(0, 14));
    }

    [Fact]
    public void Moving_right_goes_east_and_down_goes_south()
    {
        var (latitude, longitude) = MapRing.MoveTarget(35, 139, 10, 0, 100, 0);
        Assert.Equal(35, latitude, 6);
        // 100 units of the 256 * 2^10 that make 360 degrees.
        Assert.Equal(139 + 100 * 360.0 / (256 * 1024), longitude, 6);

        (latitude, longitude) = MapRing.MoveTarget(35, 139, 10, 0, 0, 100);
        Assert.True(latitude < 35);
        Assert.Equal(139, longitude, 6);
    }

    [Fact]
    public void The_same_lean_covers_half_the_ground_one_level_further_in()
    {
        var (_, at10) = MapRing.MoveTarget(0, 0, 10, 0, 100, 0);
        var (_, at11) = MapRing.MoveTarget(0, 0, 11, 0, 100, 0);
        Assert.Equal(at10 / 2, at11, 9);
    }

    [Fact]
    public void A_turned_map_still_moves_the_way_the_finger_leans()
    {
        // With east at the top of the screen (bearing 90), leaning up goes east.
        var (latitude, longitude) = MapRing.MoveTarget(0, 0, 10, 90, 0, -100);
        Assert.Equal(0, latitude, 6);
        Assert.True(longitude > 0);
    }

    [Fact]
    public void The_map_goes_round_the_world_and_stops_short_of_the_poles()
    {
        var (_, longitude) = MapRing.MoveTarget(0, 179.9, 2, 0, 50, 0);
        Assert.InRange(longitude, -180, -150);

        var (latitude, _) = MapRing.MoveTarget(84, 0, 2, 0, 0, -5000);
        Assert.Equal(85, latitude, 6);
    }
}
