using FlagsRally.Helpers;
using Maui.GoogleMaps;

namespace FlagsRallyTests.Helpers;

public class CheckInRangeTests
{
    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.05, true)]
    [InlineData(0.0501, false)]
    [InlineData(1.2, false)]
    public void A_spot_must_be_within_50_metres(double distanceKm, bool isWithin)
    {
        Assert.Equal(isWithin, CheckInRange.IsWithin(distanceKm));
    }

    [Theory]
    [InlineData(0.0321, "33 m")]
    [InlineData(0.0499, "50 m")]  // inside: shown as the limit at most
    [InlineData(0.0501, "51 m")]  // outside: never shown as 50 m
    [InlineData(1.25, "1.3 km")]
    public void Distances_are_shown_to_the_metre_and_rounded_up(double distanceKm, string expected)
    {
        Assert.Equal(expected, CheckInRange.FormatDistance(distanceKm));
    }

    [Fact]
    public void The_limit_reads_as_50_metres()
    {
        Assert.Equal("50 m", CheckInRange.LimitText);
    }

    // A pin 0.0009° north is about 100 m away: outside the range
    [Fact]
    public void The_distance_from_a_pin_is_in_kilometres()
    {
        var here = new Location(35.0, 135.0);
        Assert.Equal(0, CheckInRange.DistanceKm(new Position(35.0, 135.0), here), 6);
        var distance = CheckInRange.DistanceKm(new Position(35.0009, 135.0), here);
        Assert.InRange(distance, 0.09, 0.11);
        Assert.False(CheckInRange.IsWithin(distance));
    }
}
