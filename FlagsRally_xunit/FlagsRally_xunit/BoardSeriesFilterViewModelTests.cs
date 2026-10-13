using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class BoardSeriesFilterViewModelTests
{
    static (BoardSeriesFilterViewModel ViewModel, HashSet<string> Shown) Create(params string[] shown)
    {
        var visible = new HashSet<string>(shown);
        var boards = new[] { "Manhole 1", "Manhole 2", "Manhole 3" }.Select(n => new BoardSeriesFilterItem(n, visible.Contains(n)));
        var viewModel = new BoardSeriesFilterViewModel("Manhole", boards, (names, show) =>
        {
            if (show) visible.UnionWith(names); else visible.ExceptWith(names);
        });
        return (viewModel, visible);
    }

    [Fact]
    public void Clear_selection_hides_every_board_even_when_only_some_are_ticked()
    {
        var (viewModel, shown) = Create("Manhole 1", "Manhole 3");

        viewModel.HideAllCommand.Execute(null);

        Assert.Empty(shown);
        Assert.All(viewModel.Boards, b => Assert.False(b.IsShown));
    }

    [Fact]
    public void Select_all_shows_every_board()
    {
        var (viewModel, shown) = Create("Manhole 2");

        viewModel.ShowAllCommand.Execute(null);

        Assert.Equal(3, shown.Count);
        Assert.All(viewModel.Boards, b => Assert.True(b.IsShown));
    }

    [Fact]
    public void Ticking_a_board_shows_only_that_board()
    {
        var (viewModel, shown) = Create();

        viewModel.ToggleBoardCommand.Execute(viewModel.Boards[1]);

        Assert.Equal(["Manhole 2"], shown);
    }
}
