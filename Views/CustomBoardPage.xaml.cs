using FlagsRally.Helpers;
using FlagsRally.Models.CustomBoard;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomBoardPage : InitializingPage
{
    public const string Route = "CustomBoard";

    readonly CustomBoardPageViewModel _customBoardPageViewModel;

    // For the tiles, which read the page's settings (tile size, date shown) through the page.
    public CustomBoardPageViewModel ViewModel => _customBoardPageViewModel;

    public CustomBoardPage(CustomBoardPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = _customBoardPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
          
        _customBoardPageViewModel.UpdateTileSize(width, GridSpan.IsOpened(this));
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

    // A tap selects the tile; show it on the map and clear the selection so it can be tapped again.
    void OnTileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CustomLocation location) return;

        collectionView.SelectedItem = null;
        _customBoardPageViewModel.ShowOnMapCommand.Execute(location);
    }
}