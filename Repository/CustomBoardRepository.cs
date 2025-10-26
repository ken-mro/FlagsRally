using FlagsRally.Models.CustomBoard;
using SQLite;

namespace FlagsRally.Repository;

public class CustomBoardRepository : BaseRepository, ICustomBoardRepository
{
    protected override async Task CreateTableAsync()
    {
        await _conn!.CreateTableAsync<CustomBoardData>();
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
