using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class BoardCatalogPage : ContentPage
{
    public const string Route = "BoardCatalog";

    readonly BoardCatalogPageViewModel _viewModel;

    public BoardCatalogPage(BoardCatalogPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Init();
    }

    // The system back button leaves a subfolder before it leaves the page.
    protected override bool OnBackButtonPressed() => _viewModel.GoUp() || base.OnBackButtonPressed();
}
