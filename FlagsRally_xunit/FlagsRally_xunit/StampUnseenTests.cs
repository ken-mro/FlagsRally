using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Services;
using Moq;
using SQLite;
using System.Net;

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

    sealed class FakeHandler(Func<HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer());
    }

    static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    StampViewer Viewer(Func<HttpResponseMessage> answer) => new(_repository, new StampImageLoader(new FakeHandler(answer)));

    [Fact]
    public async Task A_pending_stamp_is_not_shown_on_the_board()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        var place = await Saved();
        Assert.True(place.IsStampPending);
        Assert.Null(place.StampImageUrl);
    }

    [Fact]
    public async Task A_seen_stamp_is_shown_on_the_board()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now);

        var place = await Saved();
        Assert.False(place.IsStampPending);
        Assert.Equal("https://example.com/01.png", place.StampImageUrl);
    }

    [Fact]
    public async Task Loading_a_pending_stamp_marks_it_seen()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        var image = await Viewer(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) })
            .LoadPendingStampAsync(await Saved());

        Assert.Equal(Png, image);
        Assert.False((await Saved()).IsStampPending);
    }

    [Fact]
    public async Task A_pending_stamp_that_still_cannot_load_stays_pending()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);

        var image = await Viewer(() => throw new HttpRequestException("offline")).LoadPendingStampAsync(await Saved());

        Assert.Null(image);
        Assert.True((await Saved()).IsStampPending);
    }

    [Fact]
    public async Task Pressing_a_pending_stamp_on_its_tile_shows_it_and_plays_the_press_once()
    {
        await _repository.UpdateCustomLocation(Key, DateTime.Now, stampUnseen: true);
        var place = await Saved();
        var changed = new List<string?>();
        place.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        place.PressStamp(Png);

        Assert.False(place.IsStampPending);
        Assert.NotNull(place.PressedStamp);
        Assert.Null(place.StampImageUrl); // shown from the downloaded bytes, not fetched again
        Assert.Contains(nameof(CustomLocation.IsStampPending), changed);
        Assert.Contains(nameof(CustomLocation.PressedStamp), changed);
        Assert.True(place.TakePressAnimation());
        Assert.False(place.TakePressAnimation());
    }
}
