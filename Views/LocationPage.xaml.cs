using FlagsRally.Models;
using FlagsRally.ViewModels;
using Maui.GoogleMaps;

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