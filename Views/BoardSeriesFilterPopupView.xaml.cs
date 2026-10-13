using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class BoardSeriesFilterPopupView : Popup
{
    public BoardSeriesFilterPopupView(BoardSeriesFilterViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;

        // A long series (31 manhole card issues) scrolls inside the popup instead of running off the screen.
        var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;
        boards.MaximumHeightRequest = screenHeight * 0.5;
    }
}
