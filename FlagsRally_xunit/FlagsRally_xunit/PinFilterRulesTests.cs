using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;

namespace FlagsRallyTests.Models;

public class PinFilterRulesTests
{
    private static List<PinFilterChip> Build(params string[] visibleKeys) =>
        PinFilterRules.Build([.. CustomBoardPinFilterItem.CreateFilterList(), new("Board A"), new("Board B")], visibleKeys);

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

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board A"));

        Assert.Equal(["Board A"], keys);
        Assert.False(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_another_board_adds_it()
    {
        var chips = Build("Board A");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board B"));

        Assert.Equal(["Board A", "Board B"], keys);
    }

    [Fact]
    public void Deselecting_the_last_board_falls_back_to_all_pins()
    {
        var chips = Build("Board A");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.Name == "Board A"));

        Assert.Empty(keys);
        Assert.True(chips.Single(c => c.IsAll).IsSelected);
    }

    [Fact]
    public void Tapping_all_pins_clears_boards()
    {
        var chips = Build("Board A", "Board B");

        var keys = PinFilterRules.Toggle(chips, chips.Single(c => c.IsAll));

        Assert.Empty(keys);
        Assert.All(chips.Where(c => !c.IsAll), c => Assert.False(c.IsSelected));
    }
}
