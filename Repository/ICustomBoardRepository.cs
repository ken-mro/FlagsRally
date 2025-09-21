using FlagsRally.Models.CustomBoard;
using System.Text.Json;

namespace FlagsRally.Repository;

public interface ICustomBoardRepository
{
    Task<int> InsertOrReplaceAsync(CustomBoard customBoardData);
    Task<int> InsertOrReplaceWithExtensionDataAsync(CustomBoard customBoard, Dictionary<string, JsonElement>? extensionData);
    Task<IEnumerable<CustomBoard>> GetAllCustomBoards();
    Task<bool> GetCustomBoardExists();
    Task<Dictionary<string, JsonElement>?> GetExtensionData(string boardName);
}
