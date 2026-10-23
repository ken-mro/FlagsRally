using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// A design in the dropdown of Settings > Passport stamps > Same design for every stamp.
/// </summary>
public partial class StampDesignChoice(StampPattern pattern) : ObservableObject
{
    public StampPattern Pattern { get; } = pattern;
    public string Name { get; } = TitleOf(pattern);

    // Ticked in the open list
    [ObservableProperty]
    bool _isSelected;

    // One set per settings page, so the ticks belong to that page
    public static List<StampDesignChoice> All() => [.. StampPatterns.All.Select(pattern => new StampDesignChoice(pattern))];

    static string TitleOf(StampPattern pattern) => pattern switch
    {
        StampPattern.Airmail => AppResources.StampPatternAirmail,
        StampPattern.CompassHexagon => AppResources.StampPatternCompassHexagon,
        StampPattern.Sunrise => AppResources.StampPatternSunrise,
        StampPattern.ScallopedGlobe => AppResources.StampPatternScallopedGlobe,
        StampPattern.WorldOval => AppResources.StampPatternWorldOval,
        StampPattern.Ticket => AppResources.StampPatternTicket,
        StampPattern.CompassSeal => AppResources.StampPatternCompassSeal,
        StampPattern.Triangle => AppResources.StampPatternTriangle,
        StampPattern.StarSeal => AppResources.StampPatternStarSeal,
        StampPattern.GlobePostmark => AppResources.StampPatternGlobePostmark,
        StampPattern.Journey => AppResources.StampPatternJourney,
        StampPattern.PostageStamp => AppResources.StampPatternPostageStamp,
        StampPattern.MovieTicket => AppResources.StampPatternMovieTicket,
        _ => AppResources.StampPatternSquare,
    };
}
