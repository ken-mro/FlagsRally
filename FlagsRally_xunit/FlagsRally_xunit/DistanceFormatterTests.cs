using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class DistanceFormatterTests
{
    [Theory]
    [InlineData(0.001, "10 m")]
    [InlineData(0.054, "50 m")]
    [InlineData(0.456, "460 m")]
    [InlineData(1.25, "1.3 km")]
    [InlineData(9.94, "9.9 km")]
    [InlineData(123.4, "123 km")]
    public void Formats_distance(double kilometers, string expected)
    {
        Assert.Equal(expected, DistanceFormatter.Format(kilometers));
    }
}
