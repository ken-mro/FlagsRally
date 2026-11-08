using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;

namespace FlagsRally.ViewModels;

public enum ArrivalInfoChoice { Close, ShowOnMap, Delete }

/// <summary>
/// A passport stamp's details, with Show on the map, Delete and Close.
/// The caller reads <see cref="Choice"/> once the popup has closed.
/// </summary>
public partial class ArrivalInfoPopupViewModel(ArrivalLocation arrivalLocation) : BaseViewModel
{
    public Popup? Popup { get; set; }

    public ArrivalInfoChoice Choice { get; private set; } = ArrivalInfoChoice.Close;

    public string Date => arrivalLocation.ArrivalDate.ToString();
    public string CountryName => arrivalLocation.CountryName;
    public string AdminAreaName => arrivalLocation.AdminAreaName;
    public string LocalityName => arrivalLocation.LocalityName;
    public string Coordinates =>
        $"{Math.Round(arrivalLocation.Location.Latitude, 6)}, {Math.Round(arrivalLocation.Location.Longitude, 6)}";

    [RelayCommand]
    Task ShowOnMapAsync() => CloseWithAsync(ArrivalInfoChoice.ShowOnMap);

    [RelayCommand]
    Task DeleteAsync() => CloseWithAsync(ArrivalInfoChoice.Delete);

    [RelayCommand]
    Task CloseAsync() => CloseWithAsync(ArrivalInfoChoice.Close);

    async Task CloseWithAsync(ArrivalInfoChoice choice)
    {
        Choice = choice;
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
