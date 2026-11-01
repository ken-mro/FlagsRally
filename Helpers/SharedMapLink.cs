using System.Globalization;
using System.Text.RegularExpressions;

namespace FlagsRally.Helpers;

/// <summary>
/// Reads a place out of what another map app shares: a link, sometimes with a name and address before it.
/// Google Maps shares a short link (maps.app.goo.gl) that has to be followed; where it leads may carry
/// the coordinates or only the place's name and address.
/// </summary>
public static partial class SharedMapLink
{
    /// <summary>The first web link in the shared text.</summary>
    public static Uri? FindLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = LinkPattern().Match(text);
        return match.Success && Uri.TryCreate(match.Value, UriKind.Absolute, out var uri) ? uri : null;
    }

    /// <summary>A short link that has to be followed to find the place.</summary>
    public static bool IsShortLink(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && (uri.Host == "maps.app.goo.gl" || (uri.Host == "goo.gl" && uri.AbsolutePath.StartsWith("/maps", StringComparison.Ordinal)));

    /// <summary>
    /// Whether a link may be followed: only Google Maps' own pages over https, since the text comes from
    /// another app and the request should not go anywhere else.
    /// </summary>
    public static bool IsGoogleMaps(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && (IsShortLink(uri) || GoogleHostPattern().IsMatch(uri.Host));

    /// <summary>The coordinates written in a link, or null when it has none.</summary>
    public static Location? FindCoordinates(string? link)
    {
        if (string.IsNullOrEmpty(link)) return null;
        var text = Uri.UnescapeDataString(link.Replace('+', ' '));

        // The place's own position comes first; "@" is only where the map was looking
        foreach (var pattern in new[] { PlacePattern(), QueryPattern(), SearchPattern(), GeoPattern(), CameraPattern() })
        {
            var match = pattern.Match(text);
            if (match.Success && TryLocation(match.Groups["lat"].Value, match.Groups["lng"].Value) is Location location)
            {
                return location;
            }
        }
        return null;
    }

    /// <summary>
    /// The place's name and address in a link without coordinates ("/maps/place/Kinkaku-ji,+1+Kinkakujicho,...",
    /// or "?q=..."), to be looked up instead.
    /// </summary>
    public static string? FindPlaceText(Uri uri)
    {
        var place = PlacePathPattern().Match(uri.AbsolutePath);
        var text = place.Success ? place.Groups["place"].Value : QueryValue(uri, "q") ?? QueryValue(uri, "query");
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = Uri.UnescapeDataString(text.Replace('+', ' ')).Trim();
        return text.Length is > 0 and <= MaxPlaceTextLength ? text : null;
    }

    /// <summary>A short name for the pin: what comes before the address.</summary>
    public static string? ShortName(string? placeText)
    {
        if (string.IsNullOrWhiteSpace(placeText)) return null;
        var name = placeText.Split(',')[0].Trim();
        // "35.0394, 135.7292" is a position, not a name
        return name.Length == 0 || double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? null : name;
    }

    /// <summary>
    /// The name written before the link ("Tokyo Tower https://..."), as some apps share a place: the first
    /// line, when it is short enough to be a name.
    /// </summary>
    public static string? NameBeforeLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = LinkPattern().Match(text);
        if (!match.Success) return null;
        var name = text[..match.Index].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return name is { Length: > 0 and <= MaxNameLength } ? name : null;
    }

    const int MaxPlaceTextLength = 300;
    const int MaxNameLength = 60;

    static string? QueryValue(Uri uri, string key)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == key) return pair[1];
        }
        return null;
    }

    static Location? TryLocation(string lat, string lng)
    {
        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(lng, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)) return null;
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) return null;
        // geo:0,0 means "no position, search by name"
        if (latitude == 0 && longitude == 0) return null;
        return new Location(latitude, longitude);
    }

    const string Lat = @"(?<lat>-?\d{1,2}(?:\.\d+)?)";
    const string Lng = @"(?<lng>-?\d{1,3}(?:\.\d+)?)";

    [GeneratedRegex(@"https?://[^\s<>""]+")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"^(?:www\.|maps\.)?google\.(?:com|co\.[a-z]{2}|com\.[a-z]{2}|[a-z]{2})$")]
    private static partial Regex GoogleHostPattern();

    [GeneratedRegex("!3d" + Lat + "!4d" + Lng)]
    private static partial Regex PlacePattern();

    [GeneratedRegex(@"[?&](?:q|query|ll|center|destination|daddr)=(?:loc:)?\s*" + Lat + @"\s*,\s*" + Lng + @"(?:[&#]|$)")]
    private static partial Regex QueryPattern();

    [GeneratedRegex(@"/maps/(?:search|place|dir)/(?:[^/]*/)?\s*" + Lat + @"\s*,\s*" + Lng + @"(?:[/?@]|$)")]
    private static partial Regex SearchPattern();

    [GeneratedRegex(@"geo:" + Lat + "," + Lng)]
    private static partial Regex GeoPattern();

    [GeneratedRegex("@" + Lat + "," + Lng + ",")]
    private static partial Regex CameraPattern();

    [GeneratedRegex(@"^/maps/place/(?<place>[^/]+)")]
    private static partial Regex PlacePathPattern();
}
