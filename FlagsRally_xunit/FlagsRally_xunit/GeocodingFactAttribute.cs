using System.Net.Sockets;
using System.Text.RegularExpressions;
using FlagsRally.Repository;

namespace FlagsRallyTests;

/// <summary>
/// A test that calls the real Google Geocoding API. It runs only where it can: with a real
/// Google Maps key in Repository/Constants.cs (the committed file has dummy values; the real key
/// is only in a developer's working tree) and a network connection to Google. Elsewhere it is
/// skipped with the reason, instead of failing on every fresh checkout or offline machine.
/// </summary>
public sealed partial class GeocodingFactAttribute : FactAttribute
{
    static readonly Lazy<string?> SkipReason = new(FindSkipReason);

    public GeocodingFactAttribute()
    {
        if (SkipReason.Value is string reason) Skip = reason;
    }

    static string? FindSkipReason()
    {
        if (!RealGoogleKey().IsMatch(Constants.GOOGLE_MAP_API_KEY))
            return "Needs the real Google Maps key in Repository/Constants.cs (the committed one is a dummy).";

        try
        {
            using var client = new TcpClient();
            if (!client.ConnectAsync("maps.googleapis.com", 443).Wait(TimeSpan.FromSeconds(3)))
                return "maps.googleapis.com is not reachable.";
        }
        catch (Exception)
        {
            return "maps.googleapis.com is not reachable.";
        }
        return null;
    }

    // The shape of a Google API key; the value itself is never printed.
    [GeneratedRegex("^AIza[0-9A-Za-z_-]{35}$")]
    private static partial Regex RealGoogleKey();
}
