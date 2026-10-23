using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;

namespace FlagsRally.ViewModels;

/// <summary>
/// An arrival's stamp as the passport shows it, opened from its pin on the map: in the design the
/// passport gives it (Settings > Passport stamps), upright and larger, with both the country and
/// the region.
/// </summary>
public partial class ArrivalStampPopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    public ArrivalLocation Arrival { get; }

    // The region's own flag (a US state, a Japanese prefecture, ...), shown beside the country's.
    public bool HasAdminAreaFlag => !string.IsNullOrEmpty(Arrival.AdminAreaFlagSource);

    // An unexplored place has the same "unexplored" wording for the country and the region; say it once.
    public bool HasAdminAreaName => !string.IsNullOrEmpty(Arrival.AdminAreaName) && Arrival.AdminAreaName != Arrival.CountryName;

    public bool IsSquare => Arrival.StampPattern == StampPattern.Square;
    public bool IsJourney => Arrival.StampPattern == StampPattern.Journey;
    public bool IsShaped => !IsSquare && !IsJourney;

    // The journey compass has room for two lines of names: the country smaller above its region.
    public double JourneyCountryFontSize => HasAdminAreaName ? 10 : 14;

    public ArrivalStampPopupViewModel(ArrivalLocation arrival, StampStyle? style = null)
    {
        Arrival = style is { } chosen
            ? StampPatterns.Apply(arrival, chosen, tilted: false)
            : arrival;
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
