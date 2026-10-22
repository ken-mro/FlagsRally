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
