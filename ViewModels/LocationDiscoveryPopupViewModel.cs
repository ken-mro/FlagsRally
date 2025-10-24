using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

public partial class LocationDiscoveryPopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    [ObservableProperty]
    private string _imageSource = string.Empty;

    [ObservableProperty]
    private string _locationTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSupportedAdminArea))]
    private bool _isCountry = false;

    public bool IsSupportedAdminArea => !IsCountry;


    public LocationDiscoveryPopupViewModel(string imageSource, bool isCountry, string locationTitle)
    {
        ImageSource = imageSource;
        IsCountry = isCountry;
        LocationTitle = locationTitle;
    }

    [RelayCommand]
    private async Task ClosePopup()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}