using FlagsRally.Helpers;
using Maui.GoogleMaps;

namespace FlagsRallyTests.Helpers;

public class PinFanHelperTests
{
    // About 1 m of latitude / of longitude at 35°N.
    const double OneMeterLat = 1 / 111_320d;
    const double OneMeterLon = 1 / (111_320d * 0.819152);

    [Fact]
    public void Pins_far_apart_stand_upright()
    {
        var rotations = PinFanHelper.GetRotations([new(35.0, 135.0), new(35.001, 135.0)]);

        Assert.Equal([0d, 0d], rotations);
    }

    [Fact]
    public void Two_pins_on_one_spot_lean_apart_evenly()
    {
        var rotations = PinFanHelper.GetRotations([new(35.0, 135.0), new(35.0, 135.0)]);

        Assert.Equal(-rotations[0], rotations[1]);
        Assert.True(rotations[0] < 0);
    }

    [Fact]
    public void Three_pins_on_one_spot_fan_around_upright()
    {
        var rotations = PinFanHelper.GetRotations([new(35.0, 135.0), new(35.0, 135.0), new(35.0, 135.0)]);

        Assert.Equal([-PinFanHelper.MaxStepDegrees, 0d, PinFanHelper.MaxStepDegrees], rotations);
    }

    [Fact]
    public void The_western_pin_of_a_close_pair_leans_left()
    {
        var east = new Position(35.0, 135.0 + 2 * OneMeterLon);
        var west = new Position(35.0, 135.0);

        var rotations = PinFanHelper.GetRotations([east, west]);

        Assert.True(rotations[1] < 0);
        Assert.True(rotations[0] > 0);
    }

    [Fact]
    public void Pins_just_beyond_the_tolerance_are_not_grouped()
    {
        var rotations = PinFanHelper.GetRotations([new(35.0, 135.0), new(35.0 + 4 * OneMeterLat, 135.0)]);

        Assert.Equal([0d, 0d], rotations);
    }

    [Fact]
    public void A_chain_of_close_pins_is_one_group()
    {
        var rotations = PinFanHelper.GetRotations([new(35.0, 135.0), new(35.0, 135.0 + 2 * OneMeterLon), new(35.0, 135.0 + 4 * OneMeterLon)]);

        Assert.Equal([-PinFanHelper.MaxStepDegrees, 0d, PinFanHelper.MaxStepDegrees], rotations);
    }

    [Fact]
    public void A_large_group_stays_within_the_maximum_spread()
    {
        var rotations = PinFanHelper.GetRotations(Enumerable.Repeat(new Position(35.0, 135.0), 10).ToList());

        Assert.Equal(PinFanHelper.MaxSpreadDegrees, rotations.Max() - rotations.Min(), 6);
        Assert.Equal(10, rotations.Distinct().Count());
    }

    [Fact]
    public void Apply_leaves_hidden_and_draggable_pins_upright_and_out_of_the_group()
    {
        var a = new Pin { Label = "A", Position = new(35.0, 135.0) };
        var b = new Pin { Label = "B", Position = new(35.0, 135.0) };
        var hidden = new Pin { Label = "C", Position = new(35.0, 135.0), IsVisible = false, Rotation = 20f };
        var dragged = new Pin { Label = "D", Position = new(35.0, 135.0), IsDraggable = true };

        PinFanHelper.Apply([a, hidden, b, dragged]);

        Assert.Equal(-a.Rotation, b.Rotation);
        Assert.NotEqual(0f, a.Rotation);
        Assert.Equal(0f, hidden.Rotation);
        Assert.Equal(0f, dragged.Rotation);
    }

    [Fact]
    public void Apply_stands_a_pin_upright_again_once_its_neighbour_is_hidden()
    {
        var a = new Pin { Label = "A", Position = new(35.0, 135.0) };
        var b = new Pin { Label = "B", Position = new(35.0, 135.0) };
        PinFanHelper.Apply([a, b]);

        b.IsVisible = false;
        PinFanHelper.Apply([a, b]);

        Assert.Equal(0f, a.Rotation);
    }
}
