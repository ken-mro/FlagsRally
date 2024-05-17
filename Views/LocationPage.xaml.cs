using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationPage : ContentPage
{
	public LocationPage(LocationPageViewModel vm)
	{
		InitializeComponent();
		vm.ArrivalMap = map;
        BindingContext = vm;
    }
}