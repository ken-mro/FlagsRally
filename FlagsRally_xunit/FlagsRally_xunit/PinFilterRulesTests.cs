using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;

namespace FlagsRallyTests.Models;

public class PinFilterRulesTests
{
    private static readonly string[] AllKeys = ["Board A", "Board B", "Manhole 1", "Manhole 2", "Manhole 3"];

    private static List<PinFilterChip> Build(params string[] hiddenKeys) =>
        PinFilterRules.Build([new("Board A"), new("Board B"), new("Manhole", ["Manhole 1", "Manhole 2", "Manhole 3"])], hiddenKeys);

    private static PinFilterChip Chip(List<PinFilterChip> chips, string name) => chips.Single(c => c.Name == name);

    [Fact]
    public void Every_chip_is_selected_while_all_pins_are_shown()
    {
        Assert.All(Build(), c => Assert.True(c.IsSelected));
    }

    [Fact]
    public void No_chip_is_selected_once_every_pin_is_hidden()
    {
        Assert.All(Build(AllKeys), c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void All_keys_are_every_board_including_a_series_members()
    {
        Assert.Equal(AllKeys, PinFilterRules.AllKeys(Build()));
    }

    [Fact]
    public void Tapping_a_shown_board_hides_only_that_board()
    {
        var chips = Build();

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board A"), []);

        Assert.Equal(["Board A"], hidden);
        Assert.False(Chip(chips, "Board A").IsSelected);
        Assert.True(Chip(chips, "Board B").IsSelected);
    }

    [Fact]
    public void Tapping_a_hidden_board_shows_it_again()
    {
        var chips = Build("Board A", "Board B");

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board B"), ["Board A", "Board B"]);

        Assert.Equal(["Board A"], hidden);
        Assert.True(Chip(chips, "Board B").IsSelected);
    }

    [Fact]
    public void Hiding_the_last_shown_board_leaves_the_map_without_pins()
    {
        string[] onlyA = ["Board B", "Manhole 1", "Manhole 2", "Manhole 3"];
        var chips = Build(onlyA);

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board A"), onlyA);

        Assert.Equal(AllKeys.Length, hidden.Count);
        Assert.All(chips, c => Assert.False(c.IsSelected));
    }

    [Fact]
    public void Clear_all_then_one_board_shows_only_that_board()
    {
        var chips = Build(AllKeys);

        var hidden = PinFilterRules.Toggle(chips, Chip(chips, "Board B"), AllKeys);

        Assert.Equal(["Board A", "Manhole 1", "Manhole 2", "Manhole 3"], hidden);
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
    public void A_series_counts_its_boards_only_when_some_are_shown()
    {
        Assert.Equal("Manhole ▾", Chip(Build(), "Manhole").Text);
        Assert.Equal("Manhole ▾", Chip(Build(AllKeys), "Manhole").Text);

        var series = Chip(Build("Manhole 1", "Manhole 3"), "Manhole");

        Assert.True(series.IsGroup);
        Assert.True(series.IsSelected);
        Assert.Equal("Manhole 1/3 ▾", series.Text);
    }

    [Fact]
    public void Showing_every_key_again_hides_nothing()
    {
        Assert.Empty(PinFilterRules.SetShown(["Board A", "Manhole 2"], AllKeys, shown: true));
    }

    [Fact]
    public void The_select_all_button_shows_whether_all_some_or_none_are_shown()
    {
        Assert.Equal(PinsShown.All, PinFilterRules.Shown(Build(), []));
        Assert.Equal(PinsShown.Some, PinFilterRules.Shown(Build("Manhole 2"), ["Manhole 2"]));
        Assert.Equal(PinsShown.None, PinFilterRules.Shown(Build(AllKeys), AllKeys));
    }

    [Fact]
    public void The_select_all_button_clears_all_when_all_are_shown()
    {
        Assert.Equal(AllKeys, PinFilterRules.ToggleAll(Build(), []));
    }

    [Fact]
    public void The_select_all_button_shows_all_when_some_or_none_are_shown()
    {
        Assert.Empty(PinFilterRules.ToggleAll(Build("Board A"), ["Board A"]));
        Assert.Empty(PinFilterRules.ToggleAll(Build(AllKeys), AllKeys));
    }

    [Fact]
    public void A_hidden_key_of_a_deleted_board_does_not_count()
    {
        Assert.Equal(PinsShown.All, PinFilterRules.Shown(Build(), ["Deleted board"]));
    }

    [Fact]
    public void A_board_alone_is_not_a_series()
    {
        var chip = Chip(Build(), "Board A");

        Assert.False(chip.IsGroup);
        Assert.Equal("Board A", chip.Text);
    }
}
