using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class PinFilterPopupView : Popup
{
    public PinFilterPopupView(PinFilterPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;
    }

    // Let the whole row toggle the check box, not just the box itself.
    private void OnOptionTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is PinFilterOption option)
        {
            option.IsSelected = !option.IsSelected;
        }
    }
}
