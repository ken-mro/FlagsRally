using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models.CustomBoard;

namespace FlagsRally.ViewModels;

public partial class CustomLocationImagePopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    /// <summary>
    /// What the stamp shows: the image downloaded before checking in when there is one (so it is on
    /// screen at once), otherwise the board's image URL, cached like the boards' other images.
    /// </summary>
    public ImageSource? StampImage { get; }

    [ObservableProperty]
    private string _locationTitle = string.Empty;

    [ObservableProperty]
    private int _height = 0;

    [ObservableProperty]
    private int _width = 0;

    [ObservableProperty]
    private bool _isAnimationEnabled;

    public CustomLocationImagePopupViewModel(CustomLocation customLocation, bool isAnimationEnabled = true, byte[]? downloadedImage = null)
    {
        ImageUrl = customLocation.ImageUrl;
        StampImage = downloadedImage is not null
            ? ImageSource.FromStream(() => new MemoryStream(downloadedImage))
            : string.IsNullOrEmpty(ImageUrl) ? null : new UriImageSource { Uri = new Uri(ImageUrl), CachingEnabled = true };
        LocationTitle = customLocation.Title;
        Height = customLocation.Board.Height;
        Width = customLocation.Board.Width;
        IsAnimationEnabled = isAnimationEnabled;
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