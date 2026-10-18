using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using FlagsRally.Helpers;
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
    // Below this the card beside a side tab bar also covers the title bar, so its text has room
    const double MinSideCardHeight = 440;
    // The card's distance from the top and bottom edges beside a side tab bar
    const double SideMargin = 36;
    // How close the card may come to the window's edges when its text needs the room
    const double MinEdgeMargin = 8;
    // The status bar and the title bar stay above the card (the title names the tab behind it).
    static double TopSpace => DeviceInfo.Platform == DevicePlatform.iOS ? 184 : 150;

    readonly OnboardingViewModel _viewModel;
    readonly SettingsPreferences _settingsPreferences;
    readonly ShellItem? _tabs;
    readonly ShellSection? _startTab;
    bool _isClosing;
    readonly Window? _window;
    // The toolkit keeps a popup this far from the window's edges (Popup.Margin, 30 pt by default).
    // The layout above a bottom tab bar is set out with it; beside a side tab bar it is taken away,
    // or the card could not reach down to the lowest tab.
    readonly Thickness _defaultMargin;
    // Whether the card was already placed again from the layout's measured position since the last page
    // or window change: once is enough, and more would chase the layout's own size changes forever.
    bool _replacedAfterLayout;
    // Extra top padding that puts the card where it is meant to be beside a side tab bar (measured once)
    double _sideTopCorrection;

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
        // A popup is as big as its content and centred; this one spans the window's width and sits on
        // the tab bar, so its arrow can point at a tab. The window, not the display: on the iPhone Duo
        // the app's window changes size when it is folded or unfolded, even with the pages open.
        _window = Shell.Current?.Window;
        if (_window is not null) _window.SizeChanged += OnWindowSizeChanged;
        layout.SizeChanged += OnLayoutSizeChanged;
        pageGrid.SizeChanged += (_, _) => Dispatcher.Dispatch(FitTextBesideSideTabBar);
        // When the card grows to fit its text it moves without the layout changing size: the side arrow
        // follows it once it has been laid out
        card.SizeChanged += (_, _) => Dispatcher.Dispatch(PlaceSideArrowFromCard);
        // Placed again once the toolkit has wrapped the popup, so the margin reaches its border
        Loaded += (_, _) => PlaceCard();
        _defaultMargin = Margin;
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
        // Beside a side tab bar the card keeps its place from page to page (only the arrow moves), so
        // its position is not measured and corrected again here
        PlaceCard();
    }


    // The tab bar moves to its new place (bottom or side, left or right) only some time after the window
    // has changed size, as when the iPhone Duo is folded or unfolded: the card is placed again once it
    // has settled, from scratch each time.
    // Turning the Duo while folded can take UIKit a few seconds to move the tab buttons.
    static readonly TimeSpan[] SettleDelays = [TimeSpan.Zero, TimeSpan.FromMilliseconds(400), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        foreach (var delay in SettleDelays)
        {
            Dispatcher.DispatchDelayed(delay, () =>
            {
                if (_isClosing) return;
                _replacedAfterLayout = false;
                _sideTopCorrection = 0;
                _sideCardSpan = null;
                PlaceCard();
            });
        }
    }

    Size WindowSize()
    {
        if (_window is { Width: > 0, Height: > 0 }) return new Size(_window.Width, _window.Height);
        var display = DeviceDisplay.Current.MainDisplayInfo;
        return new Size(display.Width / display.Density, display.Height / display.Density);
    }

    void PlaceCard()
    {
        var size = WindowSize();
        // Always the window's width, so changing the padding below never changes the layout's size
        layout.WidthRequest = size.Width;
        if (TabBarGeometry.TabBar() is Rect bar && TabBarGeometry.IsVertical(bar))
        {
            PlaceBesideSideTabBar(size, bar);
        }
        else
        {
            layout.HeightRequest = -1;
            SetSideBySide(false);
            PlaceAboveBottomTabBar(size);
        }
    }

    // The card ends just above the tab bar; the arrow sits under the middle of the page's tab.
    void PlaceAboveBottomTabBar(Size size)
    {
        arrow.IsVisible = true;
        leftArrow.IsVisible = rightArrow.IsVisible = false;
        SetPopupMargin(_defaultMargin);
        layout.Padding = new Thickness(0, 0, 0, TabBarHeight);
        // From just below the title bar down to the arrow over the tab bar, the same on every page;
        // the screenshot takes at most about a third of it, so the text and buttons stay in view.
        SetCardHeight(Math.Max(320, size.Height - TabBarHeight - ArrowHeight - TopSpace));
        if (_viewModel.Tab is int tab)
        {
            // At the tab button itself (the iPad's bar is narrow and centred); if the buttons cannot be
            // found (Android), the tabs share the bar's width
            var centres = TabBarGeometry.TabCentres();
            var tabWidth = (size.Width - 2 * TabBarInset) / TabCount;
            var x = centres.Count == TabCount ? centres[tab].X : TabBarInset + tabWidth * (tab + 0.5);
            arrow.Margin = new Thickness(x - ArrowWidth / 2, -1, 0, 0);
        }
    }

    // The tab bar stands down a side (the iPhone Duo's outer screen: the right in portrait, the left in
    // landscape): the card fills the space beside it, from below the title bar to the bar's bottom, and
    // an arrow on that side points at the tab.
    void PlaceBesideSideTabBar(Size size, Rect bar)
    {
        var onRight = bar.Center.X > size.Width / 2;
        arrow.IsVisible = false;
        leftArrow.IsVisible = !onRight;
        rightArrow.IsVisible = onRight;
        (onRight ? rightArrow : leftArrow).Opacity = _viewModel.Tab is null ? 0 : 1;
        layout.HeightRequest = -1;
        SetPopupMargin(new Thickness(0));

        // The card: beside the bar, from below the title bar (it names the tab behind the card) to near
        // the bottom; when that is too short, as on the outer screen in landscape, it covers the title bar.
        // Its size comes from the window alone, never from measuring the popup (which follows the card).
        var (cardTop, cardBottom) = SideCardSpan(size);
        SetCardHeight(cardBottom - cardTop);
        // Wide and short (the outer screen in landscape): the screenshot beside the text, so it all fits
        SetSideBySide(cardBottom - cardTop < MinSideCardHeight);
        // The card beside a side tab bar is narrower than a phone's: a smaller picture leaves the text
        // room to show in full, even with large text
        if (cardBottom - cardTop >= MinSideCardHeight)
            screenshot.MaximumHeightRequest = Math.Max(120, (cardBottom - cardTop) * 0.26);

        // The popup centres the layout in its own area, which does not line up with the window here, so
        // the padding comes from where the layout really is (its width is fixed, so this does not move it
        // sideways) plus a one-off correction measured after the first layout (see OnLayoutSizeChanged).
        var area = TabBarGeometry.FrameInWindow(layout) ?? new Rect(0, 0, size.Width, size.Height);
        var cardLeft = onRight ? 0 : bar.Right + 2;
        var cardRight = onRight ? bar.Left - 2 : size.Width;
        var padding = new Thickness(
            Math.Max(0, cardLeft - area.Left),
            Math.Max(0, cardTop + cardBottom - size.Height + _sideTopCorrection),
            Math.Max(0, area.Right - cardRight),
            0);
        if (layout.Padding != padding) layout.Padding = padding;
        PlaceSideArrow(bar, TabBarGeometry.FrameInWindow(card)?.Top ?? cardTop);
    }

    // Worked out once per window size: measured again on another page, the title bar of the tab that
    // is just opening behind the card gives a different height, and the card would jump between pages.
    (double Top, double Bottom)? _sideCardSpan;

    (double Top, double Bottom) SideCardSpan(Size size)
    {
        if (_sideCardSpan is { } span) return span;
        var bottom = size.Height - SideMargin;
        var top = (TabBarGeometry.TitleBarBottom() ?? 96) + 12;
        if (bottom - top < MinSideCardHeight) top = SideMargin;
        _sideCardSpan = (top, bottom);
        return (top, bottom);
    }

    void PlaceSideArrowFromCard()
    {
        if ((leftArrow.IsVisible || rightArrow.IsVisible) && TabBarGeometry.TabBar() is Rect bar
            && TabBarGeometry.FrameInWindow(card) is Rect frame)
        {
            PlaceSideArrow(bar, frame.Top);
        }
    }

    // Points the side arrow at the page's tab button, measured from the card's top.
    void PlaceSideArrow(Rect bar, double cardTop)
    {
        if (_viewModel.Tab is not int tab) return;
        var sideArrow = rightArrow.IsVisible ? rightArrow : leftArrow;
        // At the tab button itself; if the buttons cannot be found, the tabs share the bar's height
        var centres = TabBarGeometry.TabCentres();
        var tabCentre = centres.Count == TabCount ? centres[tab].Y : bar.Top + bar.Height * (tab + 0.5) / TabCount;
        // Kept within the card, so the arrow never makes the layout taller
        var y = Math.Clamp(tabCentre - cardTop - ArrowWidth / 2, 0, Math.Max(0, card.HeightRequest - ArrowWidth));
        sideArrow.Margin = sideArrow == rightArrow ? new Thickness(-1, y, 0, 0) : new Thickness(0, y, -1, 0);
    }

    // Once laid out, the layout's real place in the window is known; place the card again from it.
    // The padding is placed again once (more would chase the layout's own size changes); the arrow
    // follows the card every time, which never changes the layout's size.
    void OnLayoutSizeChanged(object? sender, EventArgs e)
    {
        if (!(leftArrow.IsVisible || rightArrow.IsVisible)) return;
        Dispatcher.Dispatch(() =>
        {
            if (!_replacedAfterLayout)
            {
                _replacedAfterLayout = true;
                // Centred, so moving the card down by d takes 2d more padding above it
                if (TabBarGeometry.FrameInWindow(card) is Rect placed)
                    _sideTopCorrection += 2 * (SideCardSpan(WindowSize()).Top - placed.Top);
                PlaceCard();
            }
            else
            {
                PlaceSideArrowFromCard();
            }
        });
    }

    // The screenshot (or icon) in the left column and the text in the right one, or all in one column
    void SetSideBySide(bool sideBySide)
    {
        foreach (var picture in new View[] { screenshotFrame, icon })
        {
            Grid.SetRow(picture, sideBySide ? 0 : picture == icon ? 1 : 0);
            Grid.SetRowSpan(picture, sideBySide ? 5 : 1);
            Grid.SetColumnSpan(picture, sideBySide ? 1 : 2);
            picture.VerticalOptions = sideBySide ? LayoutOptions.Center : LayoutOptions.Fill;
        }
        var row = sideBySide ? 0 : 2;
        foreach (var text in new View[] { pageTitle, pageBody, pageNote })
        {
            Grid.SetRow(text, row++);
            Grid.SetColumn(text, sideBySide ? 1 : 0);
            Grid.SetColumnSpan(text, sideBySide ? 1 : 2);
        }
        pageGrid.RowDefinitions[^1].Height = sideBySide ? GridLength.Star : GridLength.Auto;
        // Fixed shares side by side, so a wide screenshot cannot squeeze the text into a narrow strip
        pageGrid.ColumnDefinitions[0].Width = sideBySide ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        pageGrid.ColumnDefinitions[1].Width = sideBySide ? new GridLength(2, GridUnitType.Star) : GridLength.Star;
        // Beside the text the screenshot can be taller: it no longer pushes the text down
        if (sideBySide) screenshot.MaximumHeightRequest = Math.Max(120, card.HeightRequest * 0.45);
    }

    // Beside a side tab bar the card is narrow, so large text may not fit under the picture. Then the
    // picture gets smaller, down to a minimum, and after that the card grows upwards over the title bar.
    // Only ever shrinking the picture or raising the card, so this settles after a step or two.
    const double MinSidePictureHeight = 80;
    void FitTextBesideSideTabBar()
    {
        if (!(leftArrow.IsVisible || rightArrow.IsVisible) || scroll.Height <= 0) return;
        var overflow = pageGrid.Height - scroll.Height;
        if (overflow <= 1) return;
        // Beside the text (landscape) a smaller picture gives the text no more room
        var sideBySide = Grid.GetColumnSpan(screenshotFrame) == 1;
        if (!sideBySide && screenshot.IsVisible && screenshot.MaximumHeightRequest > MinSidePictureHeight)
        {
            screenshot.MaximumHeightRequest = Math.Max(MinSidePictureHeight, screenshot.MaximumHeightRequest - overflow);
            return;
        }
        // Then the card grows: up over the title bar, then into its top and bottom margins
        if (_sideCardSpan is { } span)
        {
            var bottomLimit = WindowSize().Height - MinEdgeMargin;
            var grown = span.Top > SideMargin ? (Math.Max(SideMargin, span.Top - overflow), span.Bottom)
                : span.Top > MinEdgeMargin ? (Math.Max(MinEdgeMargin, span.Top - overflow), span.Bottom)
                : span.Bottom < bottomLimit ? (span.Top, Math.Min(bottomLimit, span.Bottom + overflow))
                : span;
            if (grown == span) return;
            _sideCardSpan = grown;
            _replacedAfterLayout = false;
            PlaceCard();
        }
    }

    // The toolkit copies Popup.Margin to the border it wraps the popup in only when it opens; the pages
    // can change layout while open (folding the iPhone Duo), so the border is set directly too.
    void SetPopupMargin(Thickness margin)
    {
        if (Margin != margin) Margin = margin;
        if (Parent is View host && host.Margin != margin) host.Margin = margin;
    }

    void SetCardHeight(double cardHeight)
    {
        card.HeightRequest = cardHeight;
        screenshot.MaximumHeightRequest = Math.Max(120, cardHeight * 0.36);
    }

    async Task FinishAsync()
    {
        if (_isClosing) return;
        _isClosing = true;

        _settingsPreferences.SetHasSeenOnboarding();
        if (_window is not null) _window.SizeChanged -= OnWindowSizeChanged;
        // Back to where the pages were opened from (the passport, or Settings)
        if (_tabs is not null && _startTab is not null)
        {
            _tabs.CurrentItem = _startTab;
        }
        await CloseAsync(CancellationToken.None);
    }
}
