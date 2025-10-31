using SQLite;

namespace FlagsRally.Repository;

public abstract class BaseRepository
{
    protected SQLiteAsyncConnection? _conn;

    protected async Task Init()
    {
        if (_conn != null) return;

        _conn = new SQLiteAsyncConnection(Constants.DataBasePath);
        await CreateTableAsync();
    }

    protected abstract Task CreateTableAsync();
}