using CountryData.Standard;
using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Repository;

namespace FlagsRally.Services;

/// <summary>
/// The built-in regional flag boards: one per supported country, listing every region with its latest arrival.
/// </summary>
public class RegionalFlagsService
{
    readonly IArrivalLocationDataRepository _arrivalLocationDataRepository;
    readonly SubRegionHelper _subRegionHelper;
    readonly CustomCountryHelper _customCountryHelper;
    readonly SettingsPreferences _settingsPreferences;

    public RegionalFlagsService(IArrivalLocationDataRepository arrivalLocationDataRepository, SubRegionHelper subRegionHelper, CustomCountryHelper customCountryHelper, SettingsPreferences settingsPreferences)
    {
        _arrivalLocationDataRepository = arrivalLocationDataRepository;
        _subRegionHelper = subRegionHelper;
        _customCountryHelper = customCountryHelper;
        _settingsPreferences = settingsPreferences;
    }

    /// <summary>
    /// Supported countries by name, with the latest visited country (or the country of residence) first.
    /// </summary>
    public List<Country> GetCountries()
    {
        var countries = Constants.SupportedSubRegionCountryCodeList
            .Select(code => _customCountryHelper.GetCountryByCode(code.ToUpper()))
            .OrderBy(x => x.CountryName)
            .ToList();

        var latestCountryCode = _settingsPreferences.GetLatestCountry();
        var countryCodeOfResidence = _settingsPreferences.GetCountryOfResidence();
        var first = countries.FirstOrDefault(c => c.CountryShortCode == latestCountryCode)
                    ?? countries.FirstOrDefault(c => c.CountryShortCode == countryCodeOfResidence);
        if (first is not null)
        {
            countries.Remove(first);
            countries.Insert(0, first);
        }
        return countries;
    }

    public Country GetCountry(string countryCode) => _customCountryHelper.GetCountryByCode(countryCode.ToUpper());

    /// <summary>
    /// Every region of the country, most recently visited first; unvisited regions have <see cref="DateTime.MinValue"/>.
    /// </summary>
    public async Task<List<SubRegion>> GetRegionsAsync(Country country)
    {
        var arrivals = await _arrivalLocationDataRepository.GetSubRegionsByCountryCode(country.CountryShortCode);
        return MergeArrivals(country, arrivals, _settingsPreferences.GetCountryOfResidence());
    }

    /// <summary>
    /// The regions of every supported country (in <see cref="GetCountries"/> order), read with one query.
    /// </summary>
    public async Task<List<(Country Country, List<SubRegion> Regions)>> GetAllRegionsAsync()
    {
        var arrivalsByCountry = (await _arrivalLocationDataRepository.GetSubRegionsOfSupportedCountries())
            .ToLookup(x => x.Code.CountryCode, StringComparer.OrdinalIgnoreCase);
        var residence = _settingsPreferences.GetCountryOfResidence();

        return GetCountries()
            .Select(country => (country, MergeArrivals(country, arrivalsByCountry[country.CountryShortCode], residence)))
            .ToList();
    }

    // Every region of the country with the latest arrival in it, most recent first.
    private List<SubRegion> MergeArrivals(Country country, IEnumerable<SubRegion> arrivals, string residence)
    {
        var regions = _subRegionHelper.GetBlankAllRegionList(country, residence);
        var regionsByKey = regions.ToDictionary(x => x.Code.lowerCountryCodeHyphenRegionCode);

        foreach (var arrival in arrivals)
        {
            var subRegionCode = arrival?.Code;

            if (string.IsNullOrEmpty(subRegionCode?.RegionCode))
            {
                var acquiredSubRegionCodeString = _customCountryHelper.GetAdminAreaCode(country.CountryShortCode, arrival?.EnAdminAreaName ?? string.Empty);
                if (string.IsNullOrEmpty(acquiredSubRegionCodeString)) continue;
            }

            if (subRegionCode is null || !regionsByKey.TryGetValue(subRegionCode.lowerCountryCodeHyphenRegionCode, out var region)) continue;

            if (region.ArrivalDate < arrival!.ArrivalDate)
            {
                region.ArrivalDate = arrival.ArrivalDate;
            }
        }

        return regions.OrderByDescending(x => x.ArrivalDate).ToList();
    }
}
