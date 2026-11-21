using FlagsRally.Helpers;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class FlagsBoardPage : InitializingPage
{
    public const string Route = "FlagsBoard";

    private readonly FlagsBoardPageViewModel _flagsBoardPageViewModel;

    // For the tiles, which read the page's settings (date shown) through the page.
    public FlagsBoardPageViewModel ViewModel => _flagsBoardPageViewModel;

    public FlagsBoardPage(FlagsBoardPageViewModel vm)
    {
        InitializeComponent();
        SideTabBar.Watch(this, OnSafeAreaChanged);
        GridSpan.WatchWindow(this, UpdateColumns);
        BindingContext = _flagsBoardPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        UpdateColumns();
        RunUnderSideTabBar();
    }

    void OnSafeAreaChanged()
    {
        UpdateColumns();
        RunUnderSideTabBar();
    }

    // The column count, from the page's width and whether the phone is opened. Worked out again
    // when the window changes size or the safe area moves, as either can come after the page's
    // new size when a foldable is opened or closed; iOS then lays the rows out again.
    void UpdateColumns()
    {
        if (Width <= 0) return;

        var span = GridSpan.For(Width, GridSpan.IsOpened(this));
        if (span == _flagsBoardPageViewModel.GridItemSpan) return;

        _flagsBoardPageViewModel.GridItemSpan = span;
        SideTabBar.LayOutAgain(collectionView);
    }

    // Beside a tab bar at the side (iPhone Duo) the tiles go on to the page's edge, under the bar.
    void RunUnderSideTabBar()
    {
        var insets = SideTabBar.InsetsOf(this);
        SideTabBar.Place(listHost, collectionView,
            insets is { } side ? SideTabBar.MarginToEdges(Thickness.Zero, side.Right, side.Bottom + PagePadding) : Thickness.Zero,
            insets is null ? 0 : SideTabBar.Height - ToolbarSpace);
    }

    // The page's padding and the list's footer for the floating toolbar, as in the XAML.
    const double PagePadding = 5;
    const double ToolbarSpace = 84;
}
