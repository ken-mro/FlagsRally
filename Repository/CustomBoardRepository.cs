using FlagsRally.Models.CustomBoard;
using SQLite;
using System.Text.Json;

namespace FlagsRally.Repository;

public class CustomBoardRepository : ICustomBoardRepository
{
    private string _dbPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private async Task Init()
    {
        if (_conn != null) return;

        _conn = new SQLiteAsyncConnection(_dbPath);
        await _conn.CreateTableAsync<CustomBoardData>();
    }

    public async Task<bool> GetCustomBoardExists()
    {
        await Init();
        var count = await _conn!.Table<CustomBoardData>().CountAsync();
        return !(count == 0);
    }

    public async Task<IEnumerable<CustomBoard>> GetAllCustomBoards()
    {
        await Init();
        var customLocationDataList = await _conn!.Table<CustomBoardData>().ToListAsync();
        return customLocationDataList.Select(GetCustomBoard).ToList();
    }

    public async Task<int> InsertOrReplaceAsync(CustomBoard customBoard)
    {
        await Init();
        var customBoardData = GetCustomBoardData(customBoard);
        return await _conn!.InsertOrReplaceAsync(customBoardData);
    }

    public async Task<int> InsertOrReplaceWithExtensionDataAsync(CustomBoard customBoard, Dictionary<string, JsonElement>? extensionData)
    {
        await Init();
        var customBoardData = GetCustomBoardData(customBoard, extensionData);
        return await _conn!.InsertOrReplaceAsync(customBoardData);
    }

    public async Task<Dictionary<string, JsonElement>?> GetExtensionData(string boardName)
    {
        await Init();
        var boardData = await _conn!.Table<CustomBoardData>()
                                   .Where(b => b.Name == boardName)
                                   .FirstOrDefaultAsync();
        
        if (boardData?.ExtensionData == null)
            return null;
            
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(boardData.ExtensionData);
    }

    private CustomBoard GetCustomBoard(CustomBoardData customBoardData)
    {
        return new CustomBoard()
        {
            Name = customBoardData.Name,
            Width = customBoardData.Width,
            Height = customBoardData.Height,
            Url = customBoardData.Url,
        };
    }

    private CustomBoardData GetCustomBoardData(CustomBoard customBoard)
    {
        return new CustomBoardData()
        {
            Name = customBoard.Name,
            Width = customBoard.Width,
            Height = customBoard.Height,
            Url = customBoard.Url,
        };
    }

    private CustomBoardData GetCustomBoardData(CustomBoard customBoard, Dictionary<string, JsonElement>? extensionData)
    {
        return new CustomBoardData()
        {
            Name = customBoard.Name,
            Width = customBoard.Width,
            Height = customBoard.Height,
            Url = customBoard.Url,
            ExtensionData = extensionData != null ? JsonSerializer.Serialize(extensionData) : null
        };
    }
}
