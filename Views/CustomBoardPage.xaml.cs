using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomBoardPage : ContentPage
{
    public const string Route = "CustomBoard";

    readonly CustomBoardPageViewModel _customBoardPageViewModel;
    public CustomBoardPage(CustomBoardPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = _customBoardPageViewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customBoardPageViewModel.Init();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
          
        _customBoardPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);
    }
}