using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class ArrivalInfoPopupView : Popup
{
    public ArrivalInfoPopupView(ArrivalInfoPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;
    }
}
