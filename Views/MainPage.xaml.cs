using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class MainPage : ContentPage
{
    private const double DEFAULT_PASSPORT_IMAGE_HEIGHT = 150;
    private readonly MainPageViewModel _mainPageViewModel;
    public MainPage(MainPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _mainPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _mainPageViewModel.PassportImageHeight = DEFAULT_PASSPORT_IMAGE_HEIGHT;
        _mainPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);   
    }

    private void collectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        double scale = 1 - (e.VerticalOffset / 1000);
        scale = Math.Max(scale, 0.5);
        _mainPageViewModel.PassportImageHeight = DEFAULT_PASSPORT_IMAGE_HEIGHT * scale;
    }
}
