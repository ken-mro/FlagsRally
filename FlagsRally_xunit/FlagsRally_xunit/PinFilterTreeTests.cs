using FlagsRally.Models;

namespace FlagsRallyTests.Models;

public class PinFilterTreeTests
{
    [Fact]
    public void Boards_of_a_series_are_grouped_in_the_place_of_its_first_board()
    {
        var rows = PinFilterTree.Build(
        [
            new("Manhole 1", "Manhole"),
            new("Board A", "Board A"),
            new("Manhole 2", "Manhole"),
        ]);

        Assert.Equal(["Manhole", "Board A"], rows.Select(r => r.Name));
        Assert.True(rows[0].IsGroup);
        Assert.Equal(["Manhole 1", "Manhole 2"], rows[0].Keys);
        Assert.False(rows[1].IsGroup);
    }

    [Fact]
    public void A_series_is_a_single_level_of_boards()
    {
        var rows = PinFilterTree.Build(
        [
            new("Arrival", null),
            new("Manhole 1", "Manhole"),
            new("Manhole 2", "Manhole"),
        ]);

        Assert.Equal(["Arrival", "Manhole"], rows.Select(r => r.Name));
        Assert.All(rows, r => Assert.Equal(0, r.Depth));
        Assert.All(rows[1].Children, c =>
        {
            Assert.False(c.IsGroup);
            Assert.Equal(1, c.Depth);
        });
    }

    [Fact]
    public void A_series_of_one_board_is_just_the_board()
    {
        var rows = PinFilterTree.Build([new("Expo board", "Expo25")]);

        var row = Assert.Single(rows);
        Assert.False(row.IsGroup);
        Assert.Equal("Expo board", row.Name);
    }

    [Fact]
    public void A_series_boards_can_be_listed_as_the_outermost_rows()
    {
        var series = PinFilterTree.Build([new("Manhole 1", "M"), new("Manhole 2", "M")]).Single();

        var rows = PinFilterTree.Rebase(series.Children);

        Assert.Equal(["Manhole 1", "Manhole 2"], rows.Select(r => r.Name));
        Assert.All(rows, r => Assert.Equal(0, r.Depth));
    }

    [Fact]
    public void Only_boards_of_opened_series_are_on_screen()
    {
        var rows = PinFilterTree.Build([new("Manhole 1", "M"), new("Manhole 2", "M"), new("Board A", null)]);

        Assert.Equal(["M", "Board A"], PinFilterTree.Visible(rows).Select(r => r.Name));

        rows[0].IsExpanded = true;
        Assert.Equal(["M", "Manhole 1", "Manhole 2", "Board A"], PinFilterTree.Visible(rows).Select(r => r.Name));
    }

    [Theory]
    [InlineData(new string[0], PinsShown.All, false)]
    [InlineData(new[] { "B" }, PinsShown.Some, true)]
    [InlineData(new[] { "A", "B", "C" }, PinsShown.None, true)]
    public void A_tap_on_a_series_fills_it_unless_every_board_is_shown(string[] hidden, PinsShown state, bool showsOnTap)
    {
        var rows = PinFilterTree.Build([new("A", "G"), new("B", "G"), new("C", "G")]);

        PinFilterTree.Refresh(rows, hidden);

        Assert.Equal(state, rows[0].State);
        Assert.Equal(showsOnTap, PinFilterTree.ShowsOnTap(rows[0]));
    }
}
