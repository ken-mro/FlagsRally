using FlagsRally.Helpers;

namespace FlagsRally.Services;

/// <summary>A place shared from another map app: where it is, and its name when the share had one.</summary>
public record SharedPlace(Location Location, string? Name);

/// <summary>
/// Finds the place in what another map app shared (see <see cref="SharedMapLink"/>): coordinates written in
/// the text or link, else those behind a Google Maps short link, else the name and address it leads to,
/// looked up with the device's own geocoder (free, unlike the Geocoding API, so it works for everyone).
/// </summary>
public class SharedPlaceResolver
{
    readonly IGeocoding _geocoding;

    public SharedPlaceResolver(IGeocoding geocoding)
    {
        _geocoding = geocoding;
    }

    const int MaxRedirects = 5;
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The place, or null when the text has none. Throws when the connection fails (see <see cref="ConnectionFailure"/>).
    /// </summary>
    public async Task<SharedPlace?> ResolveAsync(string? text)
    {
        var sharedName = SharedMapLink.NameBeforeLink(text);
        if (SharedMapLink.FindCoordinates(text) is Location written)
        {
            return new SharedPlace(written, sharedName);
        }

        var link = SharedMapLink.FindLink(text);
        if (link is null || !SharedMapLink.IsGoogleMaps(link)) return null;

        var target = SharedMapLink.IsShortLink(link) ? await FollowAsync(link) : link;
        if (target is null) return null;

        var placeText = SharedMapLink.FindPlaceText(target);
        var name = SharedMapLink.ShortName(placeText) ?? sharedName;
        if (SharedMapLink.FindCoordinates(target.OriginalString) is Location linked)
        {
            return new SharedPlace(linked, name);
        }
        if (placeText is null) return null;

        var found = (await _geocoding.GetLocationsAsync(placeText))?.FirstOrDefault();
        return found is null ? null : new SharedPlace(new Location(found.Latitude, found.Longitude), name);
    }

    // Follows the short link's redirects by hand, so that none leads away from Google Maps.
    static async Task<Uri?> FollowAsync(Uri link)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { Timeout = Timeout };

        var current = link;
        for (var i = 0; i < MaxRedirects; i++)
        {
            using var response = await httpClient.GetAsync(current, HttpCompletionOption.ResponseHeadersRead);
            var location = response.Headers.Location;
            if ((int)response.StatusCode is < 300 or >= 400 || location is null)
            {
                return current;
            }
            var next = location.IsAbsoluteUri ? location : new Uri(current, location);
            if (!SharedMapLink.IsGoogleMaps(next)) return null;
            if (!SharedMapLink.IsShortLink(next)) return next;
            current = next;
        }
        return null;
    }
}
