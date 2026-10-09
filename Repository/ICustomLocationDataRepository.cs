using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Repository;

public interface ICustomLocationDataRepository
{
    Task<IEnumerable<CustomLocationPin>> InsertOrReplace(IEnumerable<CustomLocation> customLocationList);
    Task<int> UpdateCustomLocation(string key, DateTime? now);
    Task<IEnumerable<CustomLocation>> GetAllCustomLocations();
    Task<IEnumerable<CustomLocationPin>> GetAllCustomLocationPins();
    Task<CustomLocation?> GetCustomLocationByCompositeKey(string compositeKey);
    Task<int> DeleteByBoardNameAsync(string boardName);
    /// <summary>
    /// Places checked in on boards that still exist.
    /// </summary>
    Task<int> CountVisitedAsync();
}