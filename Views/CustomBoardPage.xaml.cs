using FlagsRally.Controls;
using FlagsRally.Helpers;
using FlagsRally.Models.CustomBoard;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomBoardPage : InitializingPage, ICelebrationHost
{
    public const string Route = "CustomBoard";

    readonly CustomBoardPageViewModel _customBoardPageViewModel;

    // For the tiles, which read the page's settings (tile size, date shown) through the page.
    public CustomBoardPageViewModel ViewModel => _customBoardPageViewModel;

    public CustomBoardPage(CustomBoardPageViewModel vm)
	{
		InitializeComponent();
		SideTabBar.Watch(this, OnSafeAreaChanged);
        GridSpan.WatchWindow(this, UpdateColumns);
        BindingContext = _customBoardPageViewModel = vm;
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

    // The column count and tile size, from the page's width and whether the phone is opened.
    // Worked out again when the window changes size or the safe area moves, as either can come
    // after the page's new size when a foldable is opened or closed; iOS then lays the rows out again.
    void UpdateColumns()
    {
        if (Width <= 0) return;

        var before = _customBoardPageViewModel.GridItemSpan;
        _customBoardPageViewModel.UpdateTileSize(Width, GridSpan.IsOpened(this));
        if (_customBoardPageViewModel.GridItemSpan != before) SideTabBar.LayOutAgain(collectionView);
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

    // A tap selects the tile; show it on the map and clear the selection so it can be tapped again.
    void OnTileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CustomLocation location) return;

        collectionView.SelectedItem = null;
        _customBoardPageViewModel.ShowOnMapCommand.Execute(location);
    }

    // A pending stamp has landed on its tile
    public void Celebrate()
    {
        celebration.Progress = TimeSpan.Zero;
        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }

    void OnCelebrationCompleted(object? sender, EventArgs e)
    {
        celebration.IsAnimationEnabled = false;
        celebration.IsVisible = false;
    }
}
