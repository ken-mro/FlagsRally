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

    [ObservableProperty]
    private string _locationTitle = string.Empty;

    [ObservableProperty]
    private int _height = 0;

    [ObservableProperty]
    private int _width = 0;

    [ObservableProperty]
    private bool _isAnimationEnabled;

    public CustomLocationImagePopupViewModel(CustomLocation customLocation, bool isAnimationEnabled = true)
    {
        ImageUrl = customLocation.ImageUrl;
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