using FlagsRally.Models;
using FlagsRally.Repository;

namespace FlagsRally.Helpers;

/// <summary>
/// Where the flag pictures of countries and regions come from: flagcdn.com for countries and US
/// states, the bundled images (Resources/Images) for Washington, D.C. and the other countries' regions.
/// </summary>
public static class FlagSources
{
    /// <summary>A country's or US state's flag on flagcdn.com ("jp", "us-ca").</summary>
    public static string FlagCdnUrl(string code) => $"https://flagcdn.com/160x120/{code.ToLowerInvariant()}.png";

    /// <summary>
    /// A region's flag. <paramref name="emblem"/> picks the bundled emblem instead of the flag for
    /// the regions of countries other than the US (which have no emblems).
    /// </summary>
    public static string RegionFlag(SubRegionCode code, bool emblem = false)
    {
        var lower = code.lowerCountryCodeHyphenRegionCode;
        if (lower[0..2] == "us")
        {
            return lower == "us-dc" ? "us_dc.png" : FlagCdnUrl(lower);
        }
        if (Constants.SupportedSubRegionCountryCodeList.Contains(lower[0..2]))
        {
            return emblem ? $"{code.ImageResourceString}_emblem.png" : $"{code.ImageResourceString}.png";
        }
        throw new ArgumentException("Unexpected country's SubRegionCode");
    }
}
