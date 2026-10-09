using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _mainPageViewModel;
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _mainPageViewModel = vm;
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

    // The passport header scrolls away with the stamps; once it has, the numbers move up beside
    // the title. Only visibility changes, so the list never lays itself out again while scrolling.
    private void OnStampsScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _mainPageViewModel.IsHeaderScrolledAway = e.VerticalOffset > passportHeader.Height;
    }
}
