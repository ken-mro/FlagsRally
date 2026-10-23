namespace FlagsRally.Models;

/// <summary>
/// The colours of a country's region tiles (FlagsBoardPage): the frame, the name band and its text,
/// the date, the rule under it and ARRIVAL. The motifs drawn with them are in
/// <see cref="Controls.CountryTileFrame"/>.
/// </summary>
public sealed record CountryTileStyle(
    string CountryCode,
    Color Frame,
    Color Band,
    Color BandText,
    Color Date,
    Color Rule,
    Color Arrival,
    bool SquareCorners = false,
    bool BandInnerLine = false)
{
    public float Radius => SquareCorners ? 6 : 20;

    /// <summary>The tile's corners, for the Border of a region not visited yet.</summary>
    public CornerRadius Corners => new(Radius);

    /// <summary>The thin line inside the band (France's street plaque), transparent elsewhere.</summary>
    public Color BandLine => BandInnerLine ? BandText : Colors.Transparent;
}

public static class CountryTileStyles
{
    static readonly Color Ink = Color.FromArgb("#22252C");
    static readonly Color Paper = Color.FromArgb("#F4EEDF");
    static readonly Color Stamp = Color.FromArgb("#B94047");

    /// <summary>The look the tiles had before each country had its own.</summary>
    public static readonly CountryTileStyle Default = new("", Ink, Ink, Paper, Stamp, Ink, Ink);

    static readonly Dictionary<string, CountryTileStyle> Styles = new CountryTileStyle[]
    {
        new("JP", C("#1F2F55"), C("#1F2F55"), Paper, C("#C8402F"), C("#C8402F"), Ink),
        new("US", C("#1E3A6B"), C("#1E3A6B"), Paper, C("#B22234"), C("#B22234"), C("#1E3A6B")),
        new("CA", C("#C8202E"), C("#C8202E"), Paper, C("#C8202E"), C("#C8202E"), Ink),
        new("FR", C("#3E6B4E"), C("#1F3F8F"), Paper, C("#1F3F8F"), C("#3E6B4E"), Ink, BandInnerLine: true),
        new("DE", Ink, Ink, Paper, C("#C8102E"), C("#E2A300"), Ink, SquareCorners: true),
        new("IT", C("#A94F2B"), C("#A94F2B"), Paper, C("#2F6B3A"), C("#A94F2B"), C("#5A2B18")),
        new("ES", C("#AA151B"), C("#AA151B"), C("#F6C744"), C("#AA151B"), C("#D9A520"), Ink),
        new("PT", C("#1E4E9C"), C("#1E4E9C"), Paper, C("#1E4E9C"), C("#1E4E9C"), Ink),
        new("NL", C("#2B4C9B"), C("#2B4C9B"), Paper, C("#E8742A"), C("#2B4C9B"), C("#2B4C9B")),
        new("CH", C("#D52B1E"), C("#D52B1E"), Colors.White, C("#D52B1E"), Ink, Ink, SquareCorners: true),
        new("AT", C("#B08D3C"), Ink, C("#E3C77A"), C("#C8102E"), C("#B08D3C"), Ink),
        new("CZ", C("#6E2639"), C("#6E2639"), C("#F1DFA8"), C("#11457E"), C("#C59B4B"), C("#6E2639")),
        new("NO", C("#BA0C2F"), C("#00205B"), Paper, C("#BA0C2F"), C("#BA0C2F"), C("#00205B")),
        new("UA", C("#B5262B"), C("#1F1F1F"), Paper, C("#B5262B"), C("#B5262B"), Ink),
        new("MY", C("#7A1F2B"), C("#7A1F2B"), C("#F2C14E"), C("#7A1F2B"), C("#C9962B"), Ink),
    }.ToDictionary(style => style.CountryCode, StringComparer.OrdinalIgnoreCase);

    /// <summary>The style of a country by its two-letter code, in either case; <see cref="Default"/> for any other.</summary>
    public static CountryTileStyle For(string? countryCode) =>
        countryCode is not null && Styles.TryGetValue(countryCode, out var style) ? style : Default;

    public static IReadOnlyCollection<string> CountryCodes => Styles.Keys;

    static Color C(string hex) => Color.FromArgb(hex);
}
