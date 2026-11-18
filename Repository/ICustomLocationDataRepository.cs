using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Repository;

public interface ICustomLocationDataRepository
{
    Task<IEnumerable<CustomLocationPin>> InsertOrReplace(IEnumerable<CustomLocation> customLocationList);
    Task<int> UpdateCustomLocation(string key, DateTime? now, bool stampUnseen = false);
    /// <summary>The stamp of a check-in made without its image has now been pressed down.</summary>
    Task<int> MarkStampSeen(string key);
    Task<IEnumerable<CustomLocation>> GetAllCustomLocations();
    Task<IEnumerable<CustomLocationPin>> GetAllCustomLocationPins();
    Task<CustomLocation?> GetCustomLocationByCompositeKey(string compositeKey);
    Task<int> DeleteByBoardNameAsync(string boardName);
}