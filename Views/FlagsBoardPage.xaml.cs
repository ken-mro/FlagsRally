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
        SideTabBar.Watch(this, LayOut);
        GridSpan.WatchWindow(this, LayOut);
        BindingContext = _flagsBoardPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        LayOut();
    }

    // Opening or closing a foldable can give the page its new size before the window or the safe area
    // has its own, so this runs again when either changes.
    void LayOut()
    {
        UpdateColumns();
        RunUnderSideTabBar();
    }

    void UpdateColumns() =>
        GridSpan.Update(this, collectionView, () => _flagsBoardPageViewModel.GridItemSpan, (width, opened) => _flagsBoardPageViewModel.GridItemSpan = GridSpan.For(width, opened));

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
