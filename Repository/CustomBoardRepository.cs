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
        return customLocationDataList
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.CurrentCulture)
            .Select(GetCustomBoard)
            .ToList();
    }

    public async Task<int> InsertOrReplaceAsync(CustomBoard customBoard)
    {
        await Init();
        var customBoardData = GetCustomBoardData(customBoard);

        // Keep the position of a re-imported board; append a new one to the end.
        var existing = await _conn!.FindAsync<CustomBoardData>(customBoard.Name);
        if (existing is not null)
        {
            customBoardData.SortOrder = existing.SortOrder;
        }
        else
        {
            var count = await _conn!.Table<CustomBoardData>().CountAsync();
            var maxSortOrder = count == 0 ? -1 : await _conn!.ExecuteScalarAsync<int>("SELECT MAX(SortOrder) FROM CustomBoard");
            customBoardData.SortOrder = maxSortOrder + 1;
        }

        return await _conn!.InsertOrReplaceAsync(customBoardData);
    }

    public async Task UpdateSortOrdersAsync(IReadOnlyList<string> orderedNames)
    {
        await Init();
        await _conn!.RunInTransactionAsync(conn =>
        {
            for (int i = 0; i < orderedNames.Count; i++)
            {
                conn.Execute("UPDATE CustomBoard SET SortOrder = ? WHERE Name = ?", i, orderedNames[i]);
            }
        });
    }

    public async Task<int> DeleteAsync(string name)
    {
        await Init();
        return await _conn!.Table<CustomBoardData>().Where(x => x.Name == name).DeleteAsync();
    }

    private CustomBoard GetCustomBoard(CustomBoardData customBoardData)
    {
        return new CustomBoard()
        {
            Name = customBoardData.Name,
            Width = customBoardData.Width,
            Height = customBoardData.Height,
            Url = customBoardData.Url,
            SortOrder = customBoardData.SortOrder,
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
            SortOrder = customBoard.SortOrder,
        };
    }

}
