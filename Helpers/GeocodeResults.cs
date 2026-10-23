using Newtonsoft.Json.Linq;

namespace FlagsRally.Helpers;

/// <summary>
/// Picks the names of a place out of a Google reverse geocoding response. The response lists results
/// from the street address up to the country, each with its address components. An area's own result
/// (the one whose types include the area's type) names it best. A stray result for somewhere else can
/// turn up among the others: in October 2026 every response carried a Hanoi address, first in some.
/// So the country is the one most results agree on, and the other names come only from results in it.
/// </summary>
public static class GeocodeResults
{
    /// <summary>The place's country component.</summary>
    public static JToken? Country(JToken results)
    {
        var own = OwnComponent(results, "country");
        if (own is not null) return own;

        // No result for the country itself: the one most results are in
        return results
            .Select(result => ComponentOf(result, "country"))
            .OfType<JToken>()
            .GroupBy(country => ShortName(country))
            .OrderByDescending(group => group.Count())
            .Select(group => group.First())
            .FirstOrDefault();
    }

    /// <summary>The component of the given type (an area or a locality) in the place's country.</summary>
    public static JToken? Component(JToken results, string type, string countryCode) =>
        OwnComponent(results, type, countryCode) ?? InCountry(results, countryCode).Select(result => ComponentOf(result, type)).OfType<JToken>().FirstOrDefault();

    /// <summary>
    /// The component in another language. Its own result comes first; when that keeps the English
    /// name, another result in the country may have it translated.
    /// </summary>
    public static JToken? Translated(JToken results, string type, string enName, string countryCode)
    {
        var own = Component(results, type, countryCode);
        if (own is not null && LongName(own) != enName) return own;

        return InCountry(results, countryCode)
            .Select(result => ComponentOf(result, type))
            .OfType<JToken>()
            .FirstOrDefault(component => LongName(component) != enName) ?? own;
    }

    static JToken? OwnComponent(JToken results, string type, string? countryCode = null) =>
        (countryCode is null ? results : InCountry(results, countryCode))
            .Where(result => (result["types"] ?? new JArray()).Values<string>().Contains(type))
            .Select(result => ComponentOf(result, type))
            .OfType<JToken>()
            .FirstOrDefault();

    static IEnumerable<JToken> InCountry(JToken results, string countryCode) =>
        results.Where(result => ComponentOf(result, "country") is { } country && ShortName(country) == countryCode);

    static JToken? ComponentOf(JToken? result, string type) =>
        result?["address_components"]?.FirstOrDefault(component => (component["types"] ?? new JArray()).Values<string>().Contains(type));

    static string LongName(JToken component) => component["long_name"]?.Value<string>() ?? string.Empty;
    static string ShortName(JToken component) => component["short_name"]?.Value<string>() ?? string.Empty;
}
