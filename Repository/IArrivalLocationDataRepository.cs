using FlagsRally.Models;

namespace FlagsRally.Repository;

public interface IArrivalLocationDataRepository
{
    Task<int> Save(ArrivalLocationData arrivalLocationData);
    Task<List<ArrivalLocation>> GetAllArivalLocations();
    Task<List<ArrivalLocation>> GetAllByCountryCode(string countryCode);
    Task<int> DeleteAsync(int Id);
    Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync();
    Task<List<Location>> GetAllLocations();
    Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode);
    Task<bool> UpdateDatabase();
}
