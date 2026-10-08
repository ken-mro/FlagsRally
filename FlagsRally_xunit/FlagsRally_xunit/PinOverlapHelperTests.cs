using FlagsRally.Helpers;
using FlagsRally.Models;
using Maui.GoogleMaps;

namespace FlagsRallyTests.Helpers;

public class PinOverlapHelperTests
{
    private static Pin CreatePin(string label, double latitude, double longitude, string boardName = "Board A") => new()
    {
        Label = label,
        Position = new Position(latitude, longitude),
        Tag = MapPinTag.SetCustomLocationTag($"{boardName}-{label}", boardName, false),
    };

    [Fact]
    public void Pins_at_identical_coordinates_overlap_with_tapped_pin_first()
    {
        var a = CreatePin("A", 35.0, 135.0);
        var b = CreatePin("B", 35.0, 135.0, "Board B");
        var c = CreatePin("C", 35.1, 135.0);

        var result = PinOverlapHelper.FindOverlapping(b, [a, b, c]);

        Assert.Equal([b, a], result);
    }

    [Fact]
    public void Nearby_pins_beyond_tolerance_do_not_overlap()
    {
        var a = CreatePin("A", 35.0, 135.0);
        var b = CreatePin("B", 35.0001, 135.0); // about 11 m north

        var result = PinOverlapHelper.FindOverlapping(a, [a, b]);

        Assert.Equal([a], result);
    }

    [Fact]
    public void Hidden_pins_are_excluded()
    {
        var a = CreatePin("A", 35.0, 135.0);
        var b = CreatePin("B", 35.0, 135.0);
        b.IsVisible = false;

        var result = PinOverlapHelper.FindOverlapping(a, [a, b]);

        Assert.Equal([a], result);
    }

    [Fact]
    public void Choice_labels_include_board_and_are_unique()
    {
        var a = CreatePin("Tower", 35.0, 135.0);
        var b = CreatePin("Tower", 35.0, 135.0);
        var c = CreatePin("Tower", 35.0, 135.0, "Board B");

        var labels = PinOverlapHelper.GetChoiceLabels([a, b, c]);

        Assert.Equal(["Tower – Board A", "Tower – Board A (2)", "Tower – Board B"], labels);
    }
}
