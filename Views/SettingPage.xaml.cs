using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class SettingPage : ContentPage
{
	public SettingPage(SettingPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}