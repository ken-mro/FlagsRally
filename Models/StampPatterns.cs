namespace FlagsRally.Models;

/// <summary>The shapes a passport stamp can take. Square is the original design.</summary>
public enum StampPattern { Square, Circle, Oval, Octagon, Postage }

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
        StampPattern.Circle => Token("Navy", "#182A52"),
        StampPattern.Oval => Token("Stamp", "#B94047"),
        StampPattern.Octagon => Token("StampGreen", "#2F6B4F"),
        StampPattern.Postage => Token("StampPurple", "#5B4389"),
        _ => Token("Ink", "#22252E"),
    };

    /// <summary>
    /// Room between a shaped stamp's outline and its text: round shapes need more at the sides,
    /// where the curve comes in.
    /// </summary>
    public static Thickness PaddingFor(StampPattern pattern) => pattern switch
    {
        StampPattern.Circle => new Thickness(28, 30),
        StampPattern.Oval => new Thickness(30, 16),
        StampPattern.Octagon => new Thickness(18, 14),
        StampPattern.Postage => new Thickness(16, 14),
        _ => new Thickness(0),
    };

    public static double ArrivalFontSizeFor(StampPattern pattern) => pattern switch
    {
        StampPattern.Circle or StampPattern.Oval => 30,
        StampPattern.Square => 40,
        _ => 34,
    };

    // The dashed rules above and below the date: shorter where the shape narrows.
    public static double LongLineFor(StampPattern pattern) => pattern is StampPattern.Circle or StampPattern.Oval ? 110 : 140;
    public static double ShortLineFor(StampPattern pattern) => pattern is StampPattern.Circle or StampPattern.Oval ? 80 : 110;

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
