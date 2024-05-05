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

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _mainPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);   
    }
}
