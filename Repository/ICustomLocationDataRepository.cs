using FlagsRally.Models.CustomBoard;
using System.Text.Json;

namespace FlagsRally.Repository;

public interface ICustomLocationDataRepository
{
    Task<IEnumerable<CustomLocationPin>> InsertOrReplace(IEnumerable<CustomLocation> customLocationList);
    Task<IEnumerable<CustomLocationPin>> InsertOrReplaceWithExtensionData(IEnumerable<CustomLocation> customLocationList, CustomBoardLocationJson[] jsonLocations);
    Task<int> UpdateCustomLocation(string key, DateTime? now);
    Task<IEnumerable<CustomLocation>> GetAllCustomLocations();
    Task<IEnumerable<CustomLocationPin>> GetAllCustomLocationPins();
    Task<Dictionary<string, JsonElement>?> GetExtensionData(string compositeKey);
}