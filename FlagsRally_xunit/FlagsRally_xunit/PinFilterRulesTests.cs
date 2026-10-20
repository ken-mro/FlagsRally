using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;

namespace FlagsRallyTests.Models;

public class PinFilterRulesTests
{
    private static readonly string[] AllKeys = ["Board A", "Board B", "Manhole 1", "Manhole 2", "Manhole 3"];

    private static List<PinFilterChip> Build(params string[] hiddenKeys) =>
        PinFilterRules.Build([.. CustomBoardPinFilterItem.CreateFilterList(), new("Board A"), new("Board B"),
            new("Manhole", ["Manhole 1", "Manhole 2", "Manhole 3"])], hiddenKeys);

    private static PinFilterChip Chip(List<PinFilterChip> chips, string name) => chips.Single(c => c.Name == name);

    [Fact]
    public void Nothing_hidden_selects_all_pins_only()
    {
        var chips = Build();

        Assert.True(chips.Single(c => c.IsAll).IsSelected);
        Assert.All(chips.Where(c => !c.IsAll), c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void All_keys_are_every_board_including_a_series_members()
    {
        Assert.Equal(AllKeys, PinFilterRules.AllKeys(Build()));
    }

    [Fact]
    public void Tapping_a_board_while_all_are_shown_shows_only_that_board()
    {
        var chips = Build();

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board A"), []);

        Assert.Equal(["Board B", "Manhole 1", "Manhole 2", "Manhole 3"], hidden);
        Assert.True(Chip(chips, "Board A").IsSelected);
        Assert.False(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_another_board_adds_it()
    {
        string[] onlyA = ["Board B", "Manhole 1", "Manhole 2", "Manhole 3"];
        var chips = Build(onlyA);

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board B"), onlyA);

        Assert.Equal(["Manhole 1", "Manhole 2", "Manhole 3"], hidden);
    }

    [Fact]
    public void Hiding_the_last_shown_board_with_its_chip_goes_back_to_all_pins()
    {
        string[] onlyA = ["Board B", "Manhole 1", "Manhole 2", "Manhole 3"];
        var chips = Build(onlyA);

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board A"), onlyA);

        Assert.Empty(hidden);
        Assert.True(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_all_pins_shows_everything()
    {
        var chips = Build("Board A", "Board B");

        var hidden = PinFilterRules.Toggle(chips, chips.Single(c => c.IsAll), ["Board A", "Board B"]);

        Assert.Empty(hidden);
        Assert.All(chips.Where(c => !c.IsAll), c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void Clearing_every_key_hides_all_pins_and_no_chip_is_selected()
    {
        var hidden = PinFilterRules.SetShown([], AllKeys, shown: false);
        var chips = Build([.. hidden]);

        Assert.Equal(AllKeys, hidden);
        Assert.All(chips, c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void All_but_a_few_hides_only_those()
    {
        var hidden = PinFilterRules.SetShown([], ["Board B", "Manhole 2"], shown: false);
        var chips = Build([.. hidden]);

        Assert.Equal(["Board B", "Manhole 2"], hidden);
        Assert.True(Chip(chips, "Board A").IsSelected);
        Assert.False(Chip(chips, "Board B").IsSelected);
        Assert.Equal("Manhole 2/3 ▾", Chip(chips, "Manhole").Text);
    }

    [Fact]
    public void A_board_hidden_from_nothing_shown_shows_only_that_board()
    {
        var chips = Build(AllKeys);

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board B"), AllKeys);

        Assert.Equal(["Board A", "Manhole 1", "Manhole 2", "Manhole 3"], hidden);
    }

    [Fact]
    public void A_series_counts_its_shown_boards_only_while_filtering()
    {
        Assert.Equal("Manhole ▾", Chip(Build(), "Manhole").Text);

        var series = Chip(Build("Manhole 1", "Manhole 3", "Board A"), "Manhole");

        Assert.True(series.IsGroup);
        Assert.True(series.IsSelected);
        Assert.Equal("Manhole 1/3 ▾", series.Text);
    }

    [Fact]
    public void Showing_every_key_again_goes_back_to_all_pins()
    {
        var hidden = PinFilterRules.SetShown(["Board A", "Manhole 2"], AllKeys, shown: true);

        Assert.Empty(hidden);
    }

    [Fact]
    public void A_board_alone_is_not_a_series()
    {
        var chip = Chip(Build(), "Board A");

        Assert.False(chip.IsGroup);
        Assert.Equal("Board A", chip.Text);
    }
}
