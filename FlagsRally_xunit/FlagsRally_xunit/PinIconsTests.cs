using FlagsRally.Models;
using FlagsRally.Repository;
using Moq;

namespace FlagsRallyTests.Models;

public class PinIconsTests
{
    [Theory]
    [InlineData(PinKind.Unvisited, PinStyle.Classic, "classic_pin")]
    [InlineData(PinKind.Visited, PinStyle.Classic, "classic_pin_arrived")]
    [InlineData(PinKind.CheckIn, PinStyle.Classic, "classic_default_pin")]
    [InlineData(PinKind.CheckInSpot, PinStyle.Classic, "classic_selected_location_pin")]
    [InlineData(PinKind.Unvisited, PinStyle.Drop, "pin")]
    [InlineData(PinKind.Visited, PinStyle.Drop, "pin_arrived")]
    [InlineData(PinKind.CheckIn, PinStyle.Drop, "default_pin")]
    [InlineData(PinKind.CheckInSpot, PinStyle.Drop, "selected_location_pin")]
    [InlineData(PinKind.Unvisited, PinStyle.Critter, "critter_pin")]
    [InlineData(PinKind.Visited, PinStyle.Critter, "critter_pin_arrived")]
    [InlineData(PinKind.CheckIn, PinStyle.Critter, "critter_default_pin")]
    [InlineData(PinKind.CheckInSpot, PinStyle.Critter, "critter_selected_location_pin")]
    public void Each_style_has_its_own_images(PinKind kind, PinStyle style, string expected)
    {
        Assert.Equal(expected, PinIcons.FileName(kind, style));
    }

    [Fact]
    public void Classic_pins_are_the_default()
    {
        var preferences = new Mock<IPreferences>();
        preferences.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string _, string defaultValue, string? _) => defaultValue);

        Assert.Equal(PinStyle.Classic, new SettingsPreferences(preferences.Object).GetPinStyle());
    }
}
