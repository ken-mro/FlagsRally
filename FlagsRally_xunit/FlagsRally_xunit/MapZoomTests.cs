using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapZoomTests
{
    [Theory]
    [InlineData(0, 14)]
    [InlineData(-40, 15)]     // up one step's distance: one level in
    [InlineData(-100, 16.5)]
    [InlineData(80, 12)]      // down: out
    public void Sliding_changes_the_zoom_with_the_finger(double totalY, double zoom)
    {
        Assert.Equal(zoom, MapZoom.ZoomFor(14, totalY), 3);
    }

    [Fact]
    public void The_zoom_stays_within_the_maps_range()
    {
        Assert.Equal(MapZoom.MaxZoom, MapZoom.ZoomFor(20, -1000));
        Assert.Equal(MapZoom.MinZoom, MapZoom.ZoomFor(3, 1000));
        Assert.Equal(MapZoom.MaxZoom, MapZoom.Step(21, 1));
        Assert.Equal(MapZoom.MinZoom, MapZoom.Step(2, -1));
        Assert.Equal(15.5, MapZoom.Step(14.5, 1));
    }

    [Theory]
    [InlineData(10, 30, 10)]
    [InlineData(-50, 30, -30)]
    [InlineData(50, 30, 30)]
    [InlineData(20, 0, 0)]
    public void The_knob_follows_the_finger_inside_the_track(double totalY, double limit, double offset)
    {
        Assert.Equal(offset, MapZoom.KnobOffset(totalY, limit));
    }
}
