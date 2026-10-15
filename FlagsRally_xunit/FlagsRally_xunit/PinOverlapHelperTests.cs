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
    public void Pins_at_identical_coordinates_overlap_in_map_order_whichever_is_tapped()
    {
        var a = CreatePin("A", 35.0, 135.0);
        var b = CreatePin("B", 35.0, 135.0, "Board B");
        var c = CreatePin("C", 35.1, 135.0);

        Assert.Equal([a, b], PinOverlapHelper.FindOverlapping(a, [a, b, c]));
        Assert.Equal([a, b], PinOverlapHelper.FindOverlapping(b, [a, b, c]));
    }

    [Fact]
    public void Same_location_on_two_boards_overlaps_even_though_pins_compare_equal()
    {
        var a = CreatePin("Kashima Jingu", 35.969045, 140.631436, "Board A");
        var b = CreatePin("Kashima Jingu", 35.969045, 140.631436, "Board B");

        var result = PinOverlapHelper.FindOverlapping(a, [a, b]);

        Assert.Equal(2, result.Count);
        Assert.Same(a, result[0]);
        Assert.Same(b, result[1]);
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

    [Fact]
    public void Choice_labels_mark_checked_in_places_with_the_date()
    {
        var visited = new Pin
        {
            Label = "Kashima Jingu",
            Position = new Position(35.0, 135.0),
            Address = "10 Oct 2026",
            Tag = MapPinTag.SetCustomLocationTag("Board A-1", "Board A", true),
        };
        var notVisited = CreatePin("Kashima Jingu", 35.0, 135.0, "Board B");

        var labels = PinOverlapHelper.GetChoiceLabels([visited, notVisited]);

        Assert.Equal(["✓ Kashima Jingu – Board A (10\u00A0Oct\u00A02026)", "Kashima Jingu – Board B"], labels);
    }
}
