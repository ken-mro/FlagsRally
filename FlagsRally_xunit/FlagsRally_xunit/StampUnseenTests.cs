using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using Moq;
using SQLite;

namespace FlagsRallyTests.Repository;

// A check-in made without its stamp's image keeps the celebration for the first time the stamp is looked at.
public class StampUnseenTests : IAsyncLifetime
{
    static readonly CustomBoard Board = new() { Name = "Shrines", Url = "https://example.com/{code}.png", Width = 100, Height = 100 };
    const string Key = "Shrines-01";

    readonly string _path = Path.Combine(Path.GetTempPath(), $"stamp-unseen-{Guid.NewGuid():N}.db3");
    TestRepository _repository = null!;

    sealed class TestRepository(ICustomBoardRepository boards, string path) : CustomLocationDataRepository(boards)
    {
        public async Task OpenAsync()
        {
            _conn = new SQLiteAsyncConnection(path);
            await CreateTableAsync();
        }

        public Task CloseAsync() => _conn!.CloseAsync();
    }

    public async Task InitializeAsync()
    {
        var boards = new Mock<ICustomBoardRepository>();
        boards.Setup(b => b.GetAllCustomBoards()).ReturnsAsync([Board]);
        _repository = new TestRepository(boards.Object, _path);
        await _repository.OpenAsync();
        await _repository.InsertOrReplace([Place()]);
    }

    public async Task DisposeAsync()
    {
        await _repository.CloseAsync();
        File.Delete(_path);
    }

    static CustomLocation Place() => new(Board, "01", "Katori", "Chiba", "Kanto", new Location(35.886, 140.528), arrivalDate: null) { SortIndex = 1 };

    async Task<CustomLocation> Saved() => (await _repository.GetCustomLocationByCompositeKey(Key))!;

    [Fact]
    public async Task A_check_in_without_the_stamp_is_marked()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        var place = await Saved();
        Assert.True(place.HasBeenVisited);
        Assert.True(place.StampUnseen);
    }

    [Fact]
    public async Task A_check_in_with_the_stamp_is_not_marked()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now);

        Assert.False((await Saved()).StampUnseen);
    }

    [Fact]
    public async Task Looking_at_the_stamp_clears_the_mark()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        await _repository.MarkStampSeen(Key);

        var place = await Saved();
        Assert.True(place.HasBeenVisited);
        Assert.False(place.StampUnseen);
    }

    [Fact]
    public async Task Resetting_the_check_in_clears_the_mark()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        await _repository.UpdateCustomLocation(Key, null, stampUnseen: true);

        var place = await Saved();
        Assert.False(place.HasBeenVisited);
        Assert.False(place.StampUnseen);
    }

    [Fact]
    public async Task Importing_the_board_again_keeps_the_mark()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        await _repository.InsertOrReplace([Place()]);

        var place = await Saved();
        Assert.True(place.HasBeenVisited);
        Assert.True(place.StampUnseen);
    }
}
