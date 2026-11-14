using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class BoardSeriesFilterViewModelTests
{
    // Arrival location, Board A, and the Manhole series of three boards in a group.
    static (BoardSeriesFilterViewModel ViewModel, HashSet<string> Hidden) Create(params string[] hidden)
    {
        var hiddenKeys = new HashSet<string>(hidden);
        var rows = PinFilterTree.Build(
        [
            new("Arrival location", null),
            new("Board A", "Board A"),
            new("Manhole 1", "Manhole"),
            new("Manhole 2", "Manhole"),
            new("Manhole 3", "Manhole"),
        ]);
        var viewModel = new BoardSeriesFilterViewModel("Pins", rows, hiddenKeys, (keys, show) =>
        {
            if (show) hiddenKeys.ExceptWith(keys); else hiddenKeys.UnionWith(keys);
            return hiddenKeys;
        });
        return (viewModel, hiddenKeys);
    }

    static PinFilterNode Row(BoardSeriesFilterViewModel viewModel, string name) => viewModel.Rows.Single(r => r.Name == name);

    [Fact]
    public void A_series_starts_closed_as_one_row()
    {
        var (viewModel, _) = Create();

        Assert.Equal(["Arrival location", "Board A", "Manhole"], viewModel.Rows.Select(r => r.Name));
    }

    [Fact]
    public void Tapping_a_series_opens_it_below_its_row_and_tapping_again_closes_it()
    {
        var (viewModel, _) = Create();

        viewModel.TapRowCommand.Execute(Row(viewModel, "Manhole"));
        Assert.Equal(["Arrival location", "Board A", "Manhole", "Manhole 1", "Manhole 2", "Manhole 3"], viewModel.Rows.Select(r => r.Name));
        Assert.All(viewModel.Rows.Skip(3), r => Assert.Equal(1, r.Depth));

        viewModel.TapRowCommand.Execute(Row(viewModel, "Manhole"));
        Assert.Equal(["Arrival location", "Board A", "Manhole"], viewModel.Rows.Select(r => r.Name));
    }

    [Fact]
    public void A_series_with_some_boards_hidden_shows_a_partly_filled_box()
    {
        var (viewModel, _) = Create("Manhole 2");

        var series = Row(viewModel, "Manhole");
        Assert.Equal(PinsShown.Some, series.State);
        Assert.True(series.IsSomeShown);
        Assert.Equal("2/3", series.CountText);
    }

    [Fact]
    public void Ticking_a_partly_shown_series_shows_all_its_boards()
    {
        var (viewModel, hidden) = Create("Manhole 2", "Board A");

        viewModel.ToggleShownCommand.Execute(Row(viewModel, "Manhole"));

        Assert.Equal(["Board A"], hidden);
        Assert.True(Row(viewModel, "Manhole").IsAllShown);
    }

    [Fact]
    public void Unticking_a_fully_shown_series_hides_all_its_boards()
    {
        var (viewModel, hidden) = Create();

        viewModel.ToggleShownCommand.Execute(Row(viewModel, "Manhole"));

        Assert.Equal(["Manhole 1", "Manhole 2", "Manhole 3"], hidden.Order());
        Assert.Equal(PinsShown.None, Row(viewModel, "Manhole").State);
    }

    [Fact]
    public void Unticking_a_board_inside_a_series_leaves_the_series_partly_shown()
    {
        var (viewModel, hidden) = Create();
        viewModel.TapRowCommand.Execute(Row(viewModel, "Manhole"));

        viewModel.TapRowCommand.Execute(Row(viewModel, "Manhole 1"));

        Assert.Equal(["Manhole 1"], hidden);
        Assert.Equal(PinsShown.Some, Row(viewModel, "Manhole").State);
    }

    [Fact]
    public void Clear_selection_hides_every_pin_even_when_only_some_are_ticked()
    {
        var (viewModel, hidden) = Create("Manhole 1", "Manhole 3");

        viewModel.HideAllCommand.Execute(null);

        Assert.Equal(5, hidden.Count);
        Assert.All(viewModel.Rows, r => Assert.Equal(PinsShown.None, r.State));
    }

    [Fact]
    public void Select_all_shows_every_pin()
    {
        var (viewModel, hidden) = Create("Manhole 2", "Arrival location");

        viewModel.ShowAllCommand.Execute(null);

        Assert.Empty(hidden);
        Assert.All(viewModel.Rows, r => Assert.True(r.IsAllShown));
    }
}
