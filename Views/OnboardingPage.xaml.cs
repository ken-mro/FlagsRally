using FlagsRally.Repository;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

/// <summary>
/// The how-to pages, opened over the app on the first launch and from Settings.
/// </summary>
public partial class OnboardingPage : ContentPage
{
    readonly OnboardingViewModel _viewModel;
    readonly SettingsPreferences _settingsPreferences;
    readonly TaskCompletionSource _closed = new();
    bool _isClosing;

    public OnboardingPage(SettingsPreferences settingsPreferences)
    {
        InitializeComponent();
        _settingsPreferences = settingsPreferences;
        BindingContext = _viewModel = new OnboardingViewModel(settingsPreferences, CloseAsync);
    }

    /// <summary>
    /// Finishes when the pages are closed (after the location permission was asked, if it was).
    /// </summary>
    public Task Closed => _closed.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshLocationPermissionAsync();
    }

    // The system back button goes back a page, and closes the pages on the first one.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.GoBack())
        {
            _ = CloseAsync();
        }
        return true;
    }

    async Task CloseAsync()
    {
        if (_isClosing) return;
        _isClosing = true;

        _settingsPreferences.SetHasSeenOnboarding();
        await Navigation.PopModalAsync();
        _closed.TrySetResult();
    }
}
