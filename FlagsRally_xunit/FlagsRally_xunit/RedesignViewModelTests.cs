using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Services;
using FlagsRally.ViewModels;
using Moq;

namespace FlagsRallyTests.ViewModels;

public class RedesignViewModelTests
{
    private static readonly CustomBoard BoardA = new() { Name = "Board A", Url = "https://example.com/{code}.png" };
    private static readonly CustomBoard BoardB = new() { Name = "Board B", Url = "https://example.com/b/{code}.png" };

    private static CustomLocation Location(CustomBoard board, string code, DateTime? arrival) =>
        new(board, code, code, string.Empty, string.Empty, new Location(35, 135), arrival);

    private static (Mock<ICustomBoardRepository> boards, Mock<ICustomLocationDataRepository> locations) CreateRepositories()
    {
        var boards = new Mock<ICustomBoardRepository>();
        boards.Setup(r => r.GetAllCustomBoards()).ReturnsAsync([BoardA, BoardB]);

        var locations = new Mock<ICustomLocationDataRepository>();
        locations.Setup(r => r.GetAllCustomLocations()).ReturnsAsync(
        [
            Location(BoardA, "a1", new DateTime(2026, 10, 1)),
            Location(BoardA, "a2", new DateTime(2026, 10, 4)),
            Location(BoardA, "a3", null),
            Location(BoardB, "b1", null),
        ]);
        return (boards, locations);
    }

    private static CustomBoardService CreateService(Mock<ICustomBoardRepository> boards, Mock<ICustomLocationDataRepository> locations) =>
        new(boards.Object, locations.Object, new CryptoService());

    // ---------- Collections ----------

    [Fact]
    public async Task Collections_cards_show_progress_latest_image_and_an_add_card()
    {
        var (boards, locations) = CreateRepositories();
        var vm = new CollectionsPageViewModel(boards.Object, locations.Object, CreateService(boards, locations));

        await vm.Init();

        Assert.Equal(3, vm.BoardCards.Count);
        var a = vm.BoardCards[0];
        Assert.Equal("Board A", a.Title);
        Assert.Equal("2 / 3", a.ProgressText);
        Assert.Equal(2d / 3, a.Progress, 3);
        Assert.Equal("https://example.com/a2.png", a.ImageUrl); // most recent check-in
        var b = vm.BoardCards[1];
        Assert.Equal("0 / 1", b.ProgressText);
        Assert.True(b.HasNoImage);
        Assert.True(vm.BoardCards[2].IsAddCard);
    }

    // ---------- Board details ----------

    private static async Task<CustomBoardPageViewModel> OpenBoardAsync(string boardName)
    {
        var (boards, locations) = CreateRepositories();
        var vm = new CustomBoardPageViewModel(CreateService(boards, locations), boards.Object, locations.Object, new MapFocusRequest());
        vm.ApplyQueryAttributes(new Dictionary<string, object> { [CustomBoardPageViewModel.BoardQueryKey] = boardName });
        await vm.Init();
        return vm;
    }

    [Fact]
    public async Task Board_details_open_the_requested_board_with_its_progress()
    {
        var vm = await OpenBoardAsync("Board A");

        Assert.Equal("Board A", vm.FilteredCustomBoard.Name);
        Assert.Equal("2 / 3", vm.ProgressText);
        Assert.Equal(3, vm.DisplayCustomLocationList.Count);
    }

    [Theory]
    [InlineData("Visited", new[] { "a2", "a1" })]
    [InlineData("Unvisited", new[] { "a3" })]
    [InlineData("All", new[] { "a2", "a1", "a3" })]
    public async Task Board_details_filter_by_visit_status(string filter, string[] expectedCodes)
    {
        var vm = await OpenBoardAsync("Board A");

        vm.SetVisitFilterCommand.Execute(filter);

        Assert.Equal(expectedCodes, vm.DisplayCustomLocationList.Select(x => x.Code));
    }

    // ---------- Manage boards ----------

    [Fact]
    public async Task Moving_a_board_saves_the_new_order()
    {
        var (boards, locations) = CreateRepositories();
        IReadOnlyList<string>? saved = null;
        boards.Setup(r => r.UpdateSortOrdersAsync(It.IsAny<IReadOnlyList<string>>()))
              .Callback<IReadOnlyList<string>>(order => saved = order)
              .Returns(Task.CompletedTask);
        var vm = new ManageCustomBoardsPageViewModel(boards.Object, CreateService(boards, locations));
        await vm.Init();

        await vm.MoveBoardAsync(0, 1);

        Assert.Equal(["Board B", "Board A"], vm.Boards.Select(x => x.Name));
        Assert.Equal(["Board B", "Board A"], saved);
    }

    [Fact]
    public async Task Moving_outside_the_list_does_nothing()
    {
        var (boards, locations) = CreateRepositories();
        var vm = new ManageCustomBoardsPageViewModel(boards.Object, CreateService(boards, locations));
        await vm.Init();

        await vm.MoveBoardAsync(0, -1);
        await vm.MoveBoardAsync(1, 1);

        Assert.Equal(["Board A", "Board B"], vm.Boards.Select(x => x.Name));
        boards.Verify(r => r.UpdateSortOrdersAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
    }

    [Fact]
    public async Task Delete_button_reflects_the_selection()
    {
        var (boards, locations) = CreateRepositories();
        var vm = new ManageCustomBoardsPageViewModel(boards.Object, CreateService(boards, locations));
        await vm.Init();
        Assert.False(vm.HasSelection);

        vm.Boards[0].IsSelected = true;
        vm.Boards[1].IsSelected = true;

        Assert.True(vm.HasSelection);
        Assert.Equal(2, vm.SelectedCount);
    }

    // ---------- Map focus request ----------

    [Fact]
    public void Map_focus_request_is_taken_once()
    {
        var request = new MapFocusRequest();
        request.Request("Board A-a1");

        Assert.Equal("Board A-a1", request.Take());
        Assert.Null(request.Take());
    }
}
