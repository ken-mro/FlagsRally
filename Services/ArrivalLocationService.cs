using FlagsRally.Models;
using FlagsRally.Utilities;

namespace FlagsRally.Services;

public class ArrivalLocationService
{
    private readonly CustomCountryHelper _countryHelper;
    public ArrivalLocationService(CustomCountryHelper countryHelper)
    {
        _countryHelper = countryHelper;
    }

    public IEnumerable<ArrivalLocation> GetAllCountriesArrivalLocations()
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
}
