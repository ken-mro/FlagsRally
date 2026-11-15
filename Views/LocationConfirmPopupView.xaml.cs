using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationConfirmPopupView : Popup
{
    public LocationConfirmPopupView(LocationConfirmPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;
    }
}
