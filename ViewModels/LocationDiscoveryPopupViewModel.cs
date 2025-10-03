using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

public partial class LocationDiscoveryPopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _locationTitle = string.Empty;

    [ObservableProperty]
    private int _height = 200;

    [ObservableProperty]
    private int _width = 300;

    [ObservableProperty]
    private bool _isAnimationEnabled;

    [ObservableProperty]
    private bool _isFirstTimeCountry;

    public LocationDiscoveryPopupViewModel(string imageUrl, string locationTitle, bool isFirstTimeCountry, bool isAnimationEnabled = true)
    {
        ImageUrl = imageUrl;
        LocationTitle = locationTitle;
        IsFirstTimeCountry = isFirstTimeCountry;
        IsAnimationEnabled = isAnimationEnabled;
        
        // Adjust size based on content type
        if (isFirstTimeCountry)
        {
            Height = 250;
            Width = 350;
        }
        else
        {
            Height = 200;
            Width = 300;
        }
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