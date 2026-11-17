using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class MapZoomTests
{
    [Fact]
    public void The_zoom_stays_within_the_maps_range()
    {
        Assert.Equal(MapZoom.MaxZoom, MapZoom.Clamp(25));
        Assert.Equal(MapZoom.MinZoom, MapZoom.Clamp(-3));
    }
}
