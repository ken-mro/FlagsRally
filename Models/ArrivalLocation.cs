using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public record ArrivalLocation
{
    public int Id { get; init; }
    public DateTime ArrivalDate { get; init; }
    public string ArrivalDateString => ArrivalDate.ToString("dd MMM yyyy");

    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CountryFlagSource { get; init; } = string.Empty;

    public string AdminAreaName { get; init; } = string.Empty;
    public string AdminAreaCode { get; init; } = string.Empty;
    public string AdminAreaFlagSource { get; init; } = string.Empty;

    public string LocalityName { get; init; } = string.Empty;
    
    public Location Location { get; init; } = new Location();

    // How the passport draws the stamp (Settings > Passport stamps); see StampPatterns.Apply.
    public StampPattern StampPattern { get; init; } = StampPattern.Square;
    public double StampTilt { get; init; }

    /// <summary>
    /// Whether two lists show the same stamps in the same order: the passport then keeps its list,
    /// and with it the place it was scrolled to. The position is compared by its coordinates
    /// (a Location is only equal to itself).
    /// </summary>
    public static bool ShowTheSame(IReadOnlyList<ArrivalLocation> stamps, IReadOnlyList<ArrivalLocation> others) =>
        stamps.Count == others.Count && stamps.Zip(others).All(pair => pair.First.ShowsTheSameAs(pair.Second));

    bool ShowsTheSameAs(ArrivalLocation other) =>
        this with { Location = other.Location } == other
        && Location?.Latitude == other.Location?.Latitude
        && Location?.Longitude == other.Location?.Longitude;
    public Thickness StampMargin => StampPatterns.MarginFor(StampTilt);
    // The text's colour: the pattern's ink, or the paper's where the text stands out of solid ink
    public Color StampInk => StampPatterns.TextInkFor(StampPattern);
    public Color StampDateInk => StampPatterns.DateInkFor(StampPattern);
    public Thickness StampPadding => StampPatterns.PaddingFor(StampPattern);
    public double StampArrivalFontSize => StampPatterns.ArrivalFontSizeFor(StampPattern);
    public bool StampShowsArrival => StampPatterns.ShowsArrival(StampPattern);
    public LayoutOptions StampAlignment => StampPatterns.IsLeftAligned(StampPattern) ? LayoutOptions.Start : LayoutOptions.Center;
    public TextAlignment StampTextAlignment => StampPatterns.IsLeftAligned(StampPattern) ? TextAlignment.Start : TextAlignment.Center;
    public double StampLongLine => StampPatterns.LongLineFor(StampPattern);
    public double StampShortLine => StampPatterns.ShortLineFor(StampPattern);
}
