using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// A row of Settings > Passport stamps: a mix, one design per country, or one pattern for every stamp.
/// </summary>
public partial class StampStyleOption(StampStyle style, string title, string hint, IReadOnlyList<StampPattern> previews) : ObservableObject
{
    public StampStyle Style { get; } = style;
    public string Title { get; } = title;
    public string Hint { get; } = hint;
    public bool HasHint => !string.IsNullOrEmpty(Hint);

    /// <summary>The small stamps drawn at the start of the row.</summary>
    public IReadOnlyList<StampPattern> Previews { get; } = previews;

    public bool IsFirst { get; init; }
    public bool ShowsDivider => !IsFirst;

    [ObservableProperty]
    bool _isSelected;

    public static List<StampStyleOption> All()
    {
        List<StampStyleOption> options =
        [
            new(StampStyle.Mixed, AppResources.StampStyleMixed, AppResources.StampStyleMixedHint,
                [StampPattern.Sunrise, StampPattern.Airmail, StampPattern.CompassHexagon]) { IsFirst = true },
            new(StampStyle.PerCountry, AppResources.StampStyleByCountry, AppResources.StampStyleByCountryHint,
                [StampPattern.Ticket, StampPattern.Ticket, StampPattern.StarSeal]),
        ];
        foreach (var pattern in StampPatterns.All)
        {
            options.Add(new(StampStyle.Single(pattern), TitleOf(pattern), string.Empty, [pattern]));
        }
        return options;
    }

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
