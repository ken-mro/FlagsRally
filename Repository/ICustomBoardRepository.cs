using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Repository;

public interface ICustomBoardRepository
{
    Task<int> InsertOrReplaceAsync(CustomBoard customBoardData);
    Task<IEnumerable<CustomBoard>> GetAllCustomBoards();
}
