using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;

namespace FlagsRallyTests.Models;

public class PinFilterRulesTests
{
    private static List<PinFilterChip> Build(params string[] visibleKeys) =>
        PinFilterRules.Build([.. CustomBoardPinFilterItem.CreateFilterList(), new("Board A"), new("Board B"),
            new("Manhole", ["Manhole 1", "Manhole 2", "Manhole 3"])], visibleKeys);

    [Fact]
    public void No_visible_keys_selects_all_pins_only()
    {
        var chips = Build();

        Assert.True(chips.Single(c => c.IsAll).IsSelected);
        Assert.All(chips.Where(c => !c.IsAll), c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void Tapping_a_board_selects_it_and_clears_all_pins()
    {
        var chips = Build();

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board A"), []);

        Assert.Equal(["Board A"], keys);
        Assert.False(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_another_board_adds_it()
    {
        var chips = Build("Board A");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board B"), ["Board A"]);

        Assert.Equal(["Board A", "Board B"], keys);
    }

    [Fact]
    public void Deselecting_the_last_board_falls_back_to_all_pins()
    {
        var chips = Build("Board A");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board A"), ["Board A"]);

        Assert.Empty(keys);
        Assert.True(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_all_pins_clears_boards()
    {
        var chips = Build("Board A", "Board B");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.IsAll), ["Board A", "Board B"]);

        Assert.Empty(keys);
        Assert.All(chips.Where(c => !c.IsAll), c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void A_series_is_selected_when_any_of_its_boards_is_shown_and_counts_them()
    {
        var chips = Build("Manhole 2", "Board A");
        var series = chips.Single(c => c.Name == "Manhole");

        Assert.True(series.IsGroup);
        Assert.True(series.IsSelected);
        Assert.Equal(1, series.SelectedCount);
        Assert.Equal("Manhole 1/3 ▾", series.Text);
        Assert.False(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Choosing_a_whole_series_and_then_one_board_less()
    {
        var all = PinFilterRules.SetShown([], ["Manhole 1", "Manhole 2", "Manhole 3"], shown: true);
        var fewer = PinFilterRules.SetShown(all, ["Manhole 2"], shown: false);

        Assert.Equal(["Manhole 1", "Manhole 2", "Manhole 3"], all);
        Assert.Equal(["Manhole 1", "Manhole 3"], fewer);
    }

    [Fact]
    public void A_board_alone_is_not_a_series()
    {
        var chip = Build().Single(c => c.Name == "Board A");

        Assert.False(chip.IsGroup);
        Assert.Equal("Board A", chip.Text);
    }
}
