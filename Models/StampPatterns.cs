namespace FlagsRally.Models;

/// <summary>
/// The designs a passport stamp can take. Square is the original; the others follow travel stamps
/// and postmarks (globe, compass, airmail edge, world map). None shows one means of travel on its
/// own: where travel appears, it is the whole row (plane, ship, train, car, on foot).
/// </summary>
public enum StampPattern
{
    Square,
    Airmail,
    CompassHexagon,
    Sunrise,
    ScallopedGlobe,
    WorldOval,
    Ticket,
    CompassSeal,
    Triangle,
    StarSeal,
}

/// <summary>
/// How the passport picks each stamp's pattern (Settings > Passport stamps): every stamp the same
/// pattern, a mix (each stamp its own), or one pattern per country.
/// </summary>
public readonly record struct StampStyle(StampPattern? Pattern, bool ByCountry)
{
    public static StampStyle Mixed { get; } = new(null, false);
    public static StampStyle PerCountry { get; } = new(null, true);
    public static StampStyle Single(StampPattern pattern) => new(pattern, false);
    public static StampStyle Default { get; } = Single(StampPattern.Square);

    public bool IsMixed => Pattern is null && !ByCountry;

    /// <summary>The key kept in preferences: "Mixed", "ByCountry" or a pattern's name.</summary>
    public string Key => ByCountry ? "ByCountry" : Pattern?.ToString() ?? "Mixed";

    public static StampStyle FromKey(string? key) => key switch
    {
        "Mixed" => Mixed,
        "ByCountry" => PerCountry,
        _ when Enum.TryParse<StampPattern>(key, out var pattern) && Enum.IsDefined(pattern) => Single(pattern),
        _ => Default,
    };
}

public static class StampPatterns
{
    public static IReadOnlyList<StampPattern> All { get; } = Enum.GetValues<StampPattern>();

    // A stamp leans by up to this much when the patterns are mixed, like one pressed by hand.
    const double MaxTilt = 4;

    /// <summary>
    /// The pattern of one stamp. A mix and the per-country choice are worked out from the arrival
    /// (or its country), so a stamp keeps its look every time the passport is opened.
    /// </summary>
    public static StampPattern For(ArrivalLocation arrival, StampStyle style)
    {
        if (style.Pattern is StampPattern pattern) return pattern;
        var seed = style.ByCountry ? arrival.CountryCode.ToUpperInvariant() : $"arrival-{arrival.Id}";
        return All[(int)(StableHash(seed) % (uint)All.Count)];
    }

    /// <summary>How far the stamp leans, in degrees: none when every stamp has the same pattern.</summary>
    public static double TiltFor(ArrivalLocation arrival, StampStyle style)
    {
        if (style.Pattern is not null) return 0;
        var steps = StableHash($"tilt-{arrival.Id}") % 9; // -4..+4 degrees
        return (int)steps - MaxTilt;
    }

    /// <summary>Room round a stamp: a leaning stamp needs more, or its corners leave the screen.</summary>
    public static Thickness MarginFor(double tilt) => tilt == 0 ? new Thickness(2) : new Thickness(8);

    /// <summary>The arrival with the pattern and tilt the passport draws it with.</summary>
    public static ArrivalLocation Apply(ArrivalLocation arrival, StampStyle style) =>
        arrival with { StampPattern = For(arrival, style), StampTilt = TiltFor(arrival, style) };

    /// <summary>The ink of a pattern. Square keeps the original black with a red date.</summary>
    public static Color InkFor(StampPattern pattern) => pattern switch
    {
        StampPattern.Airmail => Token("StampInkRed", "#B8333A"),
        StampPattern.CompassHexagon => Token("StampInkGreen", "#2F6B4F"),
        StampPattern.Sunrise => Token("StampInkBrown", "#8A5A2B"),
        StampPattern.ScallopedGlobe => Token("StampInkPurple", "#5B4389"),
        StampPattern.WorldOval => Token("StampInkTeal", "#23789A"),
        StampPattern.Ticket => Token("StampInkBlue", "#24477F"),
        StampPattern.CompassSeal => Token("StampInkCrimson", "#A3262E"),
        StampPattern.Triangle => Token("StampInkOchre", "#A86F17"),
        StampPattern.StarSeal => Token("StampInkPink", "#C9406F"),
        _ => Token("Ink", "#22252E"),
    };

    /// <summary>
    /// Room between a stamp's outline and its text, where the pattern's motifs go. With the
    /// smaller flag of these designs, each comes out about as tall as the original square
    /// (about 160 points at the standard text size).
    /// </summary>
    public static Thickness PaddingFor(StampPattern pattern) => pattern switch
    {
        StampPattern.Airmail => new Thickness(20, 34, 20, 14),
        StampPattern.CompassHexagon => new Thickness(40, 46, 40, 42),
        StampPattern.Sunrise => new Thickness(40, 40, 40, 34),
        StampPattern.ScallopedGlobe => new Thickness(38, 40, 38, 42),
        StampPattern.WorldOval => new Thickness(42, 18, 42, 18),
        StampPattern.Ticket => new Thickness(24, 36, 24, 46),
        StampPattern.CompassSeal => new Thickness(42, 40, 42, 44),
        StampPattern.Triangle => new Thickness(40, 62, 40, 14),
        StampPattern.StarSeal => new Thickness(40, 36, 40, 30),
        _ => new Thickness(0),
    };

    /// <summary>
    /// Whether the stamp says ARRIVAL. The designs whose motifs need room above and below the text
    /// (a globe, a compass, the row of travel) leave it out to keep the original stamp's size.
    /// </summary>
    public static bool ShowsArrival(StampPattern pattern) =>
        pattern is StampPattern.Square or StampPattern.Airmail or StampPattern.WorldOval;

    public static double ArrivalFontSizeFor(StampPattern pattern) => pattern == StampPattern.Square ? 40 : 28;

    // The dashed rules above and below the date
    public static double LongLineFor(StampPattern pattern) => pattern == StampPattern.Square ? 150 : 90;
    public static double ShortLineFor(StampPattern pattern) => pattern == StampPattern.Square ? 120 : 70;

    public static Color DateInkFor(StampPattern pattern) =>
        pattern == StampPattern.Square ? Token("Stamp", "#B94047") : InkFor(pattern);

    // The colour from Colors.xaml, or its value when there is no app (unit tests).
    static Color Token(string key, string fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);

    // FNV-1a: string.GetHashCode changes every time the app starts, which would reshuffle the stamps.
    static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }
}
