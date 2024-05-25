using FlagsRally.Models;

namespace FlagsRally.Repository;

public interface IArrivalLocationRepository
{
    Task<int> Insert(ArrivalLocation arrivalInfo);
    Task<List<ArrivalLocation>> GetAll();
    Task<List<ArrivalLocation>> GetAllByCountryCode(string countryCode);
    Task<int> DeleteAsync(int Id);
}
