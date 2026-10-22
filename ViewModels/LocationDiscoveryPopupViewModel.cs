using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

public partial class LocationDiscoveryPopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    [ObservableProperty]
    private string _imageSource = string.Empty;

    // "New country" / "New region", and on its own line the place, so a long name breaks between words
    [ObservableProperty]
    private string _heading = string.Empty;

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSupportedAdminArea))]
    private bool _isCountry = false;

    public bool IsSupportedAdminArea => !IsCountry;


    public LocationDiscoveryPopupViewModel(string imageSource, bool isCountry, string heading, string locationName)
    {
        ImageSource = imageSource;
        IsCountry = isCountry;
        Heading = heading;
        LocationName = locationName;
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