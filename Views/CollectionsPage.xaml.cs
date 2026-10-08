using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CollectionsPage : ContentPage
{
    readonly CollectionsPageViewModel _viewModel;

    public CollectionsPage(CollectionsPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    // Reload on every visit so progress reflects check-ins made on the map.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Init();
    }
}
