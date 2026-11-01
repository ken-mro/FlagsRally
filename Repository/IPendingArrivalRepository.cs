using FlagsRally.Models;

namespace FlagsRally.Repository;

public interface IPendingArrivalRepository
{
    Task<PendingArrivalData> Save(PendingArrivalData pendingArrival);
    /// <summary>Oldest first.</summary>
    Task<List<PendingArrivalData>> GetAllAsync();
    Task<int> CountAsync();
    Task<int> DeleteAsync(int id);
}
