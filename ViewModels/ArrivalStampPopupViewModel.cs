using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;

namespace FlagsRally.ViewModels;

/// <summary>
/// An arrival's stamp as the passport shows it, opened from its pin on the map: in the design the
/// passport gives it (Settings > Passport stamps), upright and larger, one page at a time like the
/// popup after a stamp: the country's stamp, then the region's when the region has one.
/// </summary>
public partial class ArrivalStampPopupViewModel(ArrivalLocation arrival, StampStyle? style = null)
    : StampSlidesViewModel(SlidesFor(arrival, style))
{
    /// <summary>
    /// The country's stamp, then the region's for a region with its own flag (a US state, a
    /// Japanese prefecture, ...). An unexplored place, whose region is named like its country, has
    /// only the one.
    /// </summary>
    public static List<StampSlide> SlidesFor(ArrivalLocation arrival, StampStyle? style = null)
    {
        var stamp = style is { } chosen ? StampPatterns.Apply(arrival, chosen, tilted: false) : arrival;
        var slides = new List<StampSlide>
        {
            new(stamp, string.Empty, stamp.CountryName, stamp.CountryFlagSource, string.Empty, celebrates: false),
        };
        var hasRegionStamp = !string.IsNullOrEmpty(stamp.AdminAreaFlagSource)
            && !string.IsNullOrEmpty(stamp.AdminAreaName)
            && stamp.AdminAreaName != stamp.CountryName;
        if (hasRegionStamp)
        {
            slides.Add(new(stamp, string.Empty, stamp.AdminAreaName, string.Empty, stamp.AdminAreaFlagSource, celebrates: false));
        }
        return slides;
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
