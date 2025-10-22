using FlagsRally.Models;
using FlagsRally.Helpers;
using FlagsRally.Repository;

namespace FlagsRally.Services;

public class ArrivalLocationService
{
    private readonly CustomCountryHelper _countryHelper;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;

    public ArrivalLocationService(CustomCountryHelper countryHelper, IArrivalLocationDataRepository arrivalLocationRepository)
    {
        _countryHelper = countryHelper;
        _arrivalLocationRepository = arrivalLocationRepository;
    }

    public IEnumerable<ArrivalLocation> GetAllCountriesArrivalLocationsForShapes()
    {
        var allCountries = _countryHelper.GetCountryData();

        var arrivalLocations = allCountries.Select(c => new ArrivalLocation()
        {
            Id = 0,
            CountryCode = c.CountryShortCode,
            CountryName = c.CountryName,
            CountryFlagSource = c.CountryFlag
        });
        return arrivalLocations;
    }

    public async Task<bool> HasVisitedCountryBefore(string countryCode)
    {
        var count = await _arrivalLocationRepository.GetAllArrivalLocations();
        return count.Any(x => x.CountryCode == countryCode);
    }

    public async Task<bool> HasVisitedSubRegionBefore(SubRegionCode subRegionCode)
    {
        var count = await _arrivalLocationRepository.GetSubRegionsByCountryCode(subRegionCode.CountryCode);
        return count.Any(x => x.Code.Equals(subRegionCode));
    }
}
