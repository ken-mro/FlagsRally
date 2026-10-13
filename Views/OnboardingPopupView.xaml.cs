using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using FlagsRally.Repository;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

/// <summary>
/// The how-to pages, opened over the app on the first launch and from Settings. They sit in a card
/// above the tab bar, which stays in sight (dimmed): each page opens its tab behind the card and points
/// at it, so it is clear which tab is which.
/// </summary>
public partial class OnboardingPopupView : Popup
{
    // The tab bar: Android's spans the screen; iOS's floats with a margin at each side.
    const int TabCount = 4;
    static double TabBarInset => DeviceInfo.Platform == DevicePlatform.iOS ? 20 : 0;
    static double TabBarHeight => DeviceInfo.Platform == DevicePlatform.iOS ? 18 : 26;
    const double ArrowWidth = 28;
    const double ArrowHeight = 16;
    // The status bar and the title bar stay above the card (the title names the tab behind it).
    static double TopSpace => DeviceInfo.Platform == DevicePlatform.iOS ? 184 : 150;

    readonly OnboardingViewModel _viewModel;
    readonly SettingsPreferences _settingsPreferences;
    readonly ShellItem? _tabs;
    readonly ShellSection? _startTab;
    bool _isClosing;
    readonly double _screenWidth;
    readonly double _screenHeight;

    OnboardingPopupView(SettingsPreferences settingsPreferences)
    {
        InitializeComponent();
        _settingsPreferences = settingsPreferences;
        _tabs = Shell.Current?.CurrentItem;
        _startTab = _tabs?.CurrentItem;
        BindingContext = _viewModel = new OnboardingViewModel(FinishAsync);

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OnboardingViewModel.Tab)) ShowTab();
        };
        // A popup is as big as its content and centred; this one spans the screen's width and sits on
        // the tab bar, so its arrow can point at a tab.
        var display = DeviceDisplay.Current.MainDisplayInfo;
        _screenWidth = display.Width / display.Density;
        _screenHeight = display.Height / display.Density;
        layout.WidthRequest = _screenWidth;
        ShowTab();
    }

    /// <summary>
    /// Opens the pages and finishes when they are closed (after the location permission was asked, if it was).
    /// </summary>
    public static async Task ShowAsync(SettingsPreferences settingsPreferences)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var popup = new OnboardingPopupView(settingsPreferences);
        await popup._viewModel.RefreshLocationPermissionAsync();
        await page.ShowPopupAsync(popup, new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = false,
            // Light enough that the screen and the tab bar below stay readable
            PageOverlayColor = Color.FromArgb("#66101A33"),
            Shape = null,
            Shadow = null,
        });
    }

    // Opens the page's tab behind the card and moves the arrow over it.
    void ShowTab()
    {
        // Hidden but keeping its place, so the card does not move on the page without a tab
        arrow.Opacity = _viewModel.Tab is null ? 0 : 1;
        if (_viewModel.Tab is int tab && _tabs is not null && tab < _tabs.Items.Count)
        {
            _tabs.CurrentItem = _tabs.Items[tab];
        }
        PlaceCard();
    }

    // The card ends just above the tab bar; the arrow sits under the middle of the page's tab.
    void PlaceCard()
    {
        layout.Padding = new Thickness(0, 0, 0, TabBarHeight);
        // From just below the title bar down to the arrow over the tab bar, the same on every page;
        // the screenshot takes at most about a third of it, so the text and buttons stay in view.
        var cardHeight = Math.Max(320, _screenHeight - TabBarHeight - ArrowHeight - TopSpace);
        card.HeightRequest = cardHeight;
        screenshot.MaximumHeightRequest = Math.Max(120, cardHeight * 0.36);
        if (_viewModel.Tab is int tab)
        {
            var tabWidth = (_screenWidth - 2 * TabBarInset) / TabCount;
            arrow.Margin = new Thickness(TabBarInset + tabWidth * (tab + 0.5) - ArrowWidth / 2, -1, 0, 0);
        }
    }

    async Task FinishAsync()
    {
        if (_isClosing) return;
        _isClosing = true;

        _settingsPreferences.SetHasSeenOnboarding();
        // Back to where the pages were opened from (the passport, or Settings)
        if (_tabs is not null && _startTab is not null)
        {
            _tabs.CurrentItem = _startTab;
        }
        await CloseAsync(CancellationToken.None);
    }
}
