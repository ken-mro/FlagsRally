using FlagsRally.Models.CustomBoard;
using SQLite;

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

}
