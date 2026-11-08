using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;
using FlagsRally.ViewModels;

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

    [Theory]
    [InlineData(VisitFilter.All, true, true)]
    [InlineData(VisitFilter.All, false, true)]
    [InlineData(VisitFilter.Visited, true, true)]
    [InlineData(VisitFilter.Visited, false, false)]
    [InlineData(VisitFilter.Unvisited, true, false)]
    [InlineData(VisitFilter.Unvisited, false, true)]
    public void A_place_follows_the_visit_filter(VisitFilter filter, bool isVisited, bool shown)
    {
        var tag = MapPinTag.SetCustomLocationTag("Board A-1", "Board A", isVisited);

        Assert.Equal(shown, PinFilterRules.IsPinShown(tag, [], filter));
    }

    [Theory]
    [InlineData(VisitFilter.All)]
    [InlineData(VisitFilter.Visited)]
    [InlineData(VisitFilter.Unvisited)]
    public void A_hidden_board_stays_hidden_whatever_the_visit_filter(VisitFilter filter)
    {
        var visited = MapPinTag.SetCustomLocationTag("Board A-1", "Board A", isVisited: true);
        var unvisited = MapPinTag.SetCustomLocationTag("Board A-2", "Board A", isVisited: false);

        Assert.False(PinFilterRules.IsPinShown(visited, ["Board A"], filter));
        Assert.False(PinFilterRules.IsPinShown(unvisited, ["Board A"], filter));
    }

    [Theory]
    [InlineData(VisitFilter.Visited)]
    [InlineData(VisitFilter.Unvisited)]
    public void Arrivals_and_untagged_pins_ignore_the_visit_filter(VisitFilter filter)
    {
        Assert.True(PinFilterRules.IsPinShown(MapPinTag.SetArrivalLocationTag(1), [], filter));
        Assert.True(PinFilterRules.IsPinShown(MapPinTag.SetPendingArrivalTag(2), [], filter));
        Assert.True(PinFilterRules.IsPinShown(null, [], filter));
    }

    [Fact]
    public void The_visit_filter_is_for_shown_boards_only()
    {
        string[] boards = ["Board A", "Board B"];

        Assert.True(PinFilterRules.AnyShown(boards, []));
        Assert.True(PinFilterRules.AnyShown(boards, ["Board A"]));
        // Only the arrival location shown, or nothing at all
        Assert.False(PinFilterRules.AnyShown(boards, ["Board A", "Board B"]));
        Assert.False(PinFilterRules.AnyShown(boards, ["Board A", "Board B", "Arrival location"]));
        // No boards on the map
        Assert.False(PinFilterRules.AnyShown([], []));
    }

    [Fact]
    public void Counts_the_places_of_shown_boards_whatever_the_visit_filter()
    {
        MapPinTag[] tags =
        [
            MapPinTag.SetCustomLocationTag("Board A-1", "Board A", isVisited: true),
            MapPinTag.SetCustomLocationTag("Board A-2", "Board A", isVisited: false),
            MapPinTag.SetCustomLocationTag("Board A-3", "Board A", isVisited: false),
            MapPinTag.SetCustomLocationTag("Board B-1", "Board B", isVisited: true),
            MapPinTag.SetArrivalLocationTag(1),
            MapPinTag.SetPendingArrivalTag(2),
        ];

        Assert.Equal((2, 4), PinFilterRules.CountPlaces(tags, []));
        Assert.Equal((1, 3), PinFilterRules.CountPlaces(tags, ["Board B"]));
        Assert.Equal((0, 0), PinFilterRules.CountPlaces(tags, ["Board A", "Board B"]));
    }
}
