using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationDiscoveryPopupView : Popup
{
    public LocationDiscoveryPopupView(LocationDiscoveryPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;
    }
}