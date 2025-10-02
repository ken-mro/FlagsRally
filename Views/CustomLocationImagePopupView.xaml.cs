using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomLocationImagePopupView : Popup
{
    public CustomLocationImagePopupView(CustomLocationImagePopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;
    }
}