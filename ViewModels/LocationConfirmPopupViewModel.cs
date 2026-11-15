using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;

namespace FlagsRally.ViewModels;

/// <summary>
/// Asks whether the place found for a stamp is right, listing it as a passport stamp's details do.
/// The caller reads <see cref="Confirmed"/> once the popup has closed: true only for Yes (No, ✕ and
/// a tap outside all leave it false).
/// </summary>
public partial class LocationConfirmPopupViewModel(string message, ArrivalLocationData place) : BaseViewModel, ILocationDetails
{
    public Popup? Popup { get; set; }

    public bool Confirmed { get; private set; }

    /// <summary>The question, such as "Is the following your current location?"</summary>
    public string Message { get; } = message;

    // The question is about the place, so no date row
    public string Date => string.Empty;
    public string CountryName => place.CountryName;
    public string AdminAreaName => place.AdminAreaName;
    public string LocalityName => place.LocalityName;
    public string Coordinates => $"{Math.Round(place.Latitude, 6)}, {Math.Round(place.Longitude, 6)}";

    [RelayCommand]
    Task YesAsync() => CloseWithAsync(true);

    [RelayCommand]
    Task NoAsync() => CloseWithAsync(false);

    async Task CloseWithAsync(bool confirmed)
    {
        Confirmed = confirmed;
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
