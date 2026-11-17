using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapZoomTests
{
    [Fact]
    public void The_zoom_stays_within_the_maps_range()
    {
        Assert.Equal(MapZoom.MaxZoom, MapZoom.Clamp(25));
        Assert.Equal(MapZoom.MinZoom, MapZoom.Clamp(-3));
        Assert.Equal(MapZoom.MaxZoom, MapZoom.Step(21, 1));
        Assert.Equal(MapZoom.MinZoom, MapZoom.Step(2, -1));
        Assert.Equal(15.5, MapZoom.Step(14.5, 1));
    }
}
