using FlagsRally.ViewModels;

namespace FlagsRally.Views;

/// <summary>
/// A page that loads its view model each time it is shown, so it reflects what changed on other
/// pages (check-ins on the map, boards added or deleted).
/// </summary>
public class InitializingPage : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IInitializable viewModel)
        {
            await viewModel.Init();
        }
    }
}
