using FlagsRally.Helpers;
using FlagsRally.Models;
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

    // Regional flags: two Japanese prefectures visited (Tokyo twice), nothing elsewhere.
    private static RegionalFlagsService CreateRegionalFlagsService()
    {
        var arrivals = new Mock<IArrivalLocationDataRepository>();
        List<SubRegion> japan =
        [
            new SubRegion { Code = new SubRegionCode("JP", "13"), ArrivalDate = new DateTime(2026, 9, 1) },
            new SubRegion { Code = new SubRegionCode("JP", "13"), ArrivalDate = new DateTime(2026, 10, 1) },
            new SubRegion { Code = new SubRegionCode("JP", "01"), ArrivalDate = new DateTime(2026, 8, 1) },
        ];
        arrivals.Setup(r => r.GetSubRegionsByCountryCode(It.IsAny<string>())).ReturnsAsync([]);
        arrivals.Setup(r => r.GetSubRegionsByCountryCode("JP")).ReturnsAsync(japan);
        arrivals.Setup(r => r.GetSubRegionsOfSupportedCountries()).ReturnsAsync(japan);
        var countryHelper = new CustomCountryHelper();
        return new RegionalFlagsService(arrivals.Object, new SubRegionHelper(countryHelper), countryHelper, CreateSettings());
    }

    // Preferences that always return the default value.
    private static SettingsPreferences CreateSettings()
    {
        var preferences = new Mock<IPreferences>();
        preferences.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string _, string defaultValue, string? _) => defaultValue);
        return new SettingsPreferences(preferences.Object);
    }

    // ---------- Collections ----------

    [Fact]
    public async Task Collections_cards_show_progress_latest_image_and_an_add_card()
    {
        var (boards, locations) = CreateRepositories();
        var vm = new CollectionsPageViewModel(boards.Object, locations.Object, CreateRegionalFlagsService(), CreateSettings());

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

    [Fact]
    public async Task Collections_show_a_card_per_country_with_visited_countries_first()
    {
        var (boards, locations) = CreateRepositories();
        var vm = new CollectionsPageViewModel(boards.Object, locations.Object, CreateRegionalFlagsService(), CreateSettings());

        await vm.Init();

        Assert.Equal(Constants.SupportedSubRegionCountryCodeList.Count, vm.RegionalCards.Count);
        var japan = vm.RegionalCards[0];
        Assert.Equal(CollectionCardKind.Regional, japan.Kind);
        Assert.Equal("JP", japan.Key);
        Assert.Equal("2 / 47", japan.ProgressText); // Tokyo counted once
        Assert.True(japan.HasLocalImage);           // the latest region's bundled emblem
        Assert.All(vm.RegionalCards.Skip(1), c => Assert.Equal(0, c.Visited));
        Assert.Equal([vm.RegionalCards, vm.BoardCards], vm.Sections);
    }

    [Fact]
    public async Task Countries_can_be_folded_away_and_the_choice_is_remembered()
    {
        var (boards, locations) = CreateRepositories();
        var preferences = new Mock<IPreferences>();
        preferences.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string _, string defaultValue, string? _) => defaultValue);
        var vm = new CollectionsPageViewModel(boards.Object, locations.Object, CreateRegionalFlagsService(), new SettingsPreferences(preferences.Object));
        await vm.Init();
        var countries = vm.RegionalCards.Count;

        vm.ToggleSectionCommand.Execute(vm.RegionalCards);

        Assert.False(vm.RegionalCards.IsExpanded);
        Assert.Empty(vm.RegionalCards);                        // only the heading is left
        Assert.Equal(countries, vm.RegionalCards.Cards.Count); // the cards come back when unfolded
        Assert.Equal("1 / " + countries, vm.RegionalCards.Summary);
        preferences.Verify(p => p.Set("RegionalFlagsExpanded", "False", It.IsAny<string>()));

        vm.ToggleSectionCommand.Execute(vm.BoardCards);        // custom boards stay open
        Assert.True(vm.BoardCards.IsExpanded);
    }

    // iOS's grouped CollectionView throws when a Reset is followed at once by item inserts.
    [Fact]
    public void Replacing_or_folding_cards_raises_a_single_reset()
    {
        var section = new CollectionSection("Boards", isEditable: true, isCollapsible: true);
        var changes = new List<System.Collections.Specialized.NotifyCollectionChangedAction>();
        section.CollectionChanged += (_, e) => changes.Add(e.Action);
        CollectionCard Card(string key) => new(CollectionCardKind.Custom, key, key, 0, 1, string.Empty, key);

        section.SetCards([Card("a"), Card("b"), Card("c")]);
        Assert.Equal([System.Collections.Specialized.NotifyCollectionChangedAction.Reset], changes);
        Assert.Equal(["a", "b", "c"], section.Select(c => c.Key));

        changes.Clear();
        section.IsExpanded = false;
        Assert.Equal([System.Collections.Specialized.NotifyCollectionChangedAction.Reset], changes);
        Assert.Empty(section);

        changes.Clear();
        section.IsExpanded = true;
        Assert.Equal([System.Collections.Specialized.NotifyCollectionChangedAction.Reset], changes);
        Assert.Equal(3, section.Count);
    }

    // ---------- Regional flags board ----------

    [Theory]
    [InlineData("All", 47)]
    [InlineData("Visited", 2)]
    [InlineData("Unvisited", 45)]
    public async Task Regional_board_opens_the_requested_country_and_filters_by_visit_status(string filter, int expectedCount)
    {
        var vm = new FlagsBoardPageViewModel(CreateRegionalFlagsService());
        vm.ApplyQueryAttributes(new Dictionary<string, object> { [FlagsBoardPageViewModel.CountryQueryKey] = "jp" });
        await vm.Init();

        vm.SetVisitFilterCommand.Execute(filter);

        Assert.Equal("JP", vm.FilteredCountry?.CountryShortCode);
        Assert.Equal("2 / 47", vm.ProgressText);
        Assert.Equal(expectedCount, vm.DisplayFullSubRegionList.Count);
    }

    // ---------- Board details ----------

    private static async Task<CustomBoardPageViewModel> OpenBoardAsync(string boardName)
    {
        var (boards, locations) = CreateRepositories();
        var vm = new CustomBoardPageViewModel(CreateService(boards, locations), boards.Object, locations.Object, new MapFocusRequest(), CreateSettings());
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

    // ---------- Sorting a custom board ----------

    // JSON order: k1 (Kanto), h1 (Hokkaido, visited 3 Oct), k2 (Kanto, visited 1 Oct), x1 (no group), h2 (Hokkaido)
    private static List<CustomLocation> SortSample() =>
    [
        new(BoardA, "k1", "k1", string.Empty, "Kanto", new Location(35, 139), null) { SortIndex = 1 },
        new(BoardA, "h1", "h1", string.Empty, "Hokkaido", new Location(43, 141), new DateTime(2026, 10, 3)) { SortIndex = 2 },
        new(BoardA, "k2", "k2", string.Empty, "Kanto", new Location(35, 139), new DateTime(2026, 10, 1)) { SortIndex = 3 },
        new(BoardA, "x1", "x1", string.Empty, string.Empty, new Location(35, 135), null) { SortIndex = 4 },
        new(BoardA, "h2", "h2", string.Empty, "Hokkaido", new Location(43, 141), null) { SortIndex = 5 },
    ];

    [Theory]
    [InlineData(LocationSort.JsonOrder, new[] { "k1", "h1", "k2", "x1", "h2" })]
    [InlineData(LocationSort.NewestFirst, new[] { "h1", "k2", "k1", "x1", "h2" })]
    [InlineData(LocationSort.OldestFirst, new[] { "k2", "h1", "k1", "x1", "h2" })]
    [InlineData(LocationSort.Group, new[] { "k1", "k2", "h1", "h2", "x1" })]
    public void Places_are_sorted_with_unvisited_ones_in_board_order(LocationSort sort, string[] expectedCodes)
    {
        var shuffled = SortSample().OrderBy(x => x.Code);

        Assert.Equal(expectedCodes, LocationSorter.Sort(shuffled, sort).Select(x => x.Code));
    }

    [Fact]
    public void Grouping_keeps_the_board_order_of_groups_and_collects_ungrouped_places()
    {
        var groups = LocationSorter.Group(SortSample(), LocationSort.Group);

        Assert.Equal(["Kanto", "Hokkaido"], groups.Take(2).Select(g => g.Name));
        Assert.Equal(["x1"], groups[2].Select(x => x.Code)); // ungrouped places last, under "Other"
        Assert.Equal("1 / 2", groups[0].ProgressText);
        Assert.All(groups, g => Assert.True(g.HasHeading));
    }

    [Fact]
    public void Other_sorts_show_one_group_without_a_heading()
    {
        var groups = LocationSorter.Group(SortSample(), LocationSort.JsonOrder);

        var only = Assert.Single(groups);
        Assert.False(only.HasHeading);
        Assert.Equal(5, only.Count);
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
