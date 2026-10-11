using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _mainPageViewModel;
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _mainPageViewModel = vm;
        toolbar.SizeChanged += (_, _) => toolbarSpace.HeightRequest = toolbar.Height + toolbar.Margin.Bottom + 8;
        passportHeader.SizeChanged += (_, _) => PlaceStats();
    }

    // Reload on every visit so new arrivals and check-ins show up without pulling to refresh.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _mainPageViewModel.Init();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _mainPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);
    }

    // The counts sit beside the passport cover when each label fits on one line there. Otherwise
    // (a foldable's cover screen with large text) they move below the cover and use the full width,
    // instead of breaking "Countries" or "チェックイン" in the middle of the word.
    private void PlaceStats()
    {
        if (passportHeader.Width <= 0) return;

        const double ColumnMargin = 8;
        var widest = new View[] { countriesStat, regionsStat, checkInsStat }
            .Max(stat => stat.Measure(double.PositiveInfinity, double.PositiveInfinity).Width) + ColumnMargin;
        var needed = 3 * widest + 2 + statsCard.Padding.HorizontalThickness + statsCard.StrokeThickness * 2;
        var beside = passportHeader.Width - passportHeader.Padding.HorizontalThickness
            - passportCover.WidthRequest - passportHeader.ColumnSpacing;
        var below = needed > beside;

        if (below == (Grid.GetRow(statsCard) == 1)) return;
        Grid.SetRow(statsCard, below ? 1 : 0);
        Grid.SetColumn(statsCard, below ? 0 : 1);
        Grid.SetColumnSpan(statsCard, below ? 2 : 1);
        Grid.SetColumnSpan(passportCover, below ? 2 : 1);
        passportCover.HorizontalOptions = below ? LayoutOptions.Center : LayoutOptions.Fill;
    }

    // The passport header scrolls away with the stamps; once its counts have, they move up beside
    // the title. Only visibility changes, so the list never lays itself out again while scrolling.
    private void OnStampsScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _mainPageViewModel.IsHeaderScrolledAway = e.VerticalOffset > statsCard.Y + statsCard.Height;
    }
}
