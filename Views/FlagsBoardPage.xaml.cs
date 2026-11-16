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
        BindingContext = _flagsBoardPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _flagsBoardPageViewModel.GridItemSpan = GridSpan.For(width, GridSpan.IsOpened(this));
        RunUnderSideTabBar();
    }

    // Beside a tab bar at the side (iPhone Duo) the tiles go on to the page's edge, under the bar.
    void RunUnderSideTabBar()
    {
        var insets = SideTabBar.InsetsOf(this);
        listHost.Margin = insets is { } side ? SideTabBar.MarginToEdges(Thickness.Zero, side.Right, side.Bottom + PagePadding) : Thickness.Zero;
        SideTabBar.SetRoomAfterLastRow(collectionView, insets is null ? 0 : SideTabBar.Height - ToolbarSpace);
    }

    // The page's padding and the list's footer for the floating toolbar, as in the XAML.
    const double PagePadding = 5;
    const double ToolbarSpace = 84;
}
