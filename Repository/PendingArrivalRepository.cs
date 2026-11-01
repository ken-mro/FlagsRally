using FlagsRally.Models;

namespace FlagsRally.Repository;

public class PendingArrivalRepository : BaseRepository, IPendingArrivalRepository
{
    protected override async Task CreateTableAsync()
    {
        await _conn!.CreateTableAsync<PendingArrivalData>();
    }

    public async Task<PendingArrivalData> Save(PendingArrivalData pendingArrival)
    {
        await Init();
        if (await _conn!.InsertAsync(pendingArrival) == 0)
        {
            throw new Exception("Failed to save PendingArrivalData");
        }
        return pendingArrival;
    }

    public async Task<List<PendingArrivalData>> GetAllAsync()
    {
        await Init();
        return await _conn!.Table<PendingArrivalData>().OrderBy(p => p.ArrivalDate).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        await Init();
        return await _conn!.Table<PendingArrivalData>().CountAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        await Init();
        return await _conn!.Table<PendingArrivalData>().Where(p => p.Id == id).DeleteAsync();
    }
}
