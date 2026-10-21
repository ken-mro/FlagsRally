namespace FlagsRally.Models;

/// <summary>
/// The designs a passport stamp can take. Square is the original; the others follow travel stamps
/// and postmarks (globe, compass, airmail edge, world map). None shows one means of travel on its
/// own: where travel appears, it is the whole row (plane, ship, train, car, on foot).
/// </summary>
public enum StampPattern
{
    Square,
    GlobeSeal,
    Airmail,
    CompassHexagon,
    Sunrise,
    ScallopedGlobe,
    WorldOval,
    Ticket,
    CompassSeal,
    Postcard,
    Triangle,
    StarSeal,
    Orbit,
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
        StampPattern.GlobeSeal => Token("StampInkNavy", "#1F3A6B"),
        StampPattern.Airmail => Token("StampInkRed", "#B8333A"),
        StampPattern.CompassHexagon => Token("StampInkGreen", "#2F6B4F"),
        StampPattern.Sunrise => Token("StampInkBrown", "#8A5A2B"),
        StampPattern.ScallopedGlobe => Token("StampInkPurple", "#5B4389"),
        StampPattern.WorldOval => Token("StampInkTeal", "#23789A"),
        StampPattern.Ticket => Token("StampInkBlue", "#24477F"),
        StampPattern.CompassSeal => Token("StampInkCrimson", "#A3262E"),
        StampPattern.Postcard => Token("StampInkOlive", "#5F6330"),
        StampPattern.Triangle => Token("StampInkOchre", "#A86F17"),
        StampPattern.StarSeal => Token("StampInkPink", "#C9406F"),
        StampPattern.Orbit => Token("StampInkGray", "#45464B"),
        _ => Token("Ink", "#22252E"),
    };

    /// <summary>
    /// Room between a stamp's outline and its text, where the pattern's motifs go: a globe or
    /// compass at the top, the row of travel at the bottom, the narrow top of a triangle.
    /// </summary>
    public static Thickness PaddingFor(StampPattern pattern) => pattern switch
    {
        StampPattern.GlobeSeal => new Thickness(30, 44, 30, 44),
        StampPattern.Airmail => new Thickness(22, 26, 22, 36),
        StampPattern.CompassHexagon => new Thickness(28, 52, 28, 50),
        StampPattern.Sunrise => new Thickness(32, 44, 32, 32),
        StampPattern.ScallopedGlobe => new Thickness(34, 50, 34, 40),
        StampPattern.WorldOval => new Thickness(44, 26, 44, 26),
        StampPattern.Ticket => new Thickness(24, 40, 24, 44),
        StampPattern.CompassSeal => new Thickness(36, 44, 36, 34),
        StampPattern.Postcard => new Thickness(24, 32, 24, 42),
        StampPattern.Triangle => new Thickness(40, 84, 40, 34),
        StampPattern.StarSeal => new Thickness(34, 44, 34, 36),
        StampPattern.Orbit => new Thickness(24, 46, 24, 44),
        _ => new Thickness(0),
    };

    public static double ArrivalFontSizeFor(StampPattern pattern) => pattern == StampPattern.Square ? 40 : 30;

    // The dashed rules above and below the date
    public static double LongLineFor(StampPattern pattern) => pattern == StampPattern.Square ? 150 : 110;
    public static double ShortLineFor(StampPattern pattern) => pattern == StampPattern.Square ? 120 : 80;

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
