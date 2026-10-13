using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class FlagsBoardPage : ContentPage
{
    public const string Route = "FlagsBoard";

    private readonly FlagsBoardPageViewModel _flagsBoardPageViewModel;

    // For the tiles, which read the page's settings (date shown) through the page.
    public FlagsBoardPageViewModel ViewModel => _flagsBoardPageViewModel;

    public FlagsBoardPage(FlagsBoardPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _flagsBoardPageViewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _flagsBoardPageViewModel.Init();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _flagsBoardPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);
    }
}
