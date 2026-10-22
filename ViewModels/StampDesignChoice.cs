using FlagsRally.Models;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// A design in the dropdown of Settings > Passport stamps > Same design for every stamp.
/// </summary>
public record StampDesignChoice(StampPattern Pattern, string Name)
{
    public static IReadOnlyList<StampDesignChoice> All { get; } =
        [.. StampPatterns.All.Select(pattern => new StampDesignChoice(pattern, TitleOf(pattern)))];

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
        _ => AppResources.StampPatternSquare,
    };
}
