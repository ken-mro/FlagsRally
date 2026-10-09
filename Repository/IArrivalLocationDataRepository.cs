using FlagsRally.Models;

namespace FlagsRally.Repository;

public interface IArrivalLocationDataRepository
{
    Task<ArrivalLocation> Save(ArrivalLocationData arrivalLocationData);
    Task<List<ArrivalLocation>> GetAllArrivalLocations();
    Task<int> DeleteAsync(int Id);
    Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync();
    Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode);
    /// <summary>
    /// Arrivals in every country that has regional flags, in one query.
    /// </summary>
    Task<List<SubRegion>> GetSubRegionsOfSupportedCountries();
    Task<int> UpdateAdminAreaCode(int Id, string adminAreaCode);
}