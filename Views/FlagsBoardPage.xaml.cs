using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class FlagsBoardPage : ContentPage
{
    private readonly FlagsBoardPageViewModel _flagsBoardPageViewModel;
    public FlagsBoardPage(FlagsBoardPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = _flagsBoardPageViewModel = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        _flagsBoardPageViewModel.GridItemSpan = Math.Max((int)width / 196, 2);
    }
}