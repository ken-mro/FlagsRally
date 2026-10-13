using FlagsRally.Models.CustomBoard;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomBoardPage : ContentPage
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customBoardPageViewModel.Init();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
          
        _customBoardPageViewModel.UpdateTileSize(width);
    }

    // A tap selects the tile; show it on the map and clear the selection so it can be tapped again.
    void OnTileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CustomLocation location) return;

        collectionView.SelectedItem = null;
        _customBoardPageViewModel.ShowOnMapCommand.Execute(location);
    }
}