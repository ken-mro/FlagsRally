using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Services;
using Moq;
using System.Text;

namespace FlagsRallyTests.Services;

public class BoardImportSafetyTests
{
    private static CustomLocation Place(string url, DateTime? arrival = null) =>
        new(new CustomBoard { Name = "Board", Url = url }, "a/b?c", "Tokyo Tower", "Tokyo", "Kanto", new Location(35, 139), arrival);

    [Fact]
    public void Image_urls_only_take_the_place_fields_and_escape_them()
    {
        var place = Place("https://img.example/{code}/{title}.jpg");

        Assert.Equal("https://img.example/a%2Fb%3Fc/Tokyo%20Tower.jpg", place.ImageUrl);
    }

    [Fact]
    public void Image_urls_never_carry_visit_dates_or_other_fields()
    {
        var place = Place("https://img.example/{code}.png?d={arrivalDate}&v={hasBeenVisited}&l={location}", new DateTime(2026, 10, 1));

        Assert.Equal("https://img.example/a%2Fb%3Fc.png?d=&v=&l=", place.ImageUrl);
    }

    private static CustomBoardJson Board(string url = "https://img.example/{code}.jpg", params string[] codes) => new()
    {
        name = "Board",
        url = url,
        width = 192,
        height = 270,
        locations = (codes.Length == 0 ? ["a", "b"] : codes).Select(c => new CustomBoardLocationJson { code = c, title = c }).ToArray(),
    };

    [Fact]
    public void A_normal_board_is_accepted()
    {
        Assert.True(CustomBoardService.IsValid(Board()));
        Assert.True(CustomBoardService.IsValid(Board(url: "")));
    }

    [Theory]
    [InlineData("file:///data/data/secret.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    public void Image_templates_must_be_web_addresses(string url)
    {
        Assert.False(CustomBoardService.IsValid(Board(url)));
    }

    [Fact]
    public void Places_need_unique_codes()
    {
        Assert.False(CustomBoardService.IsValid(Board(codes: ["a", "a"])));
        Assert.False(CustomBoardService.IsValid(Board(codes: ["a", " "])));
    }

    [Fact]
    public void A_board_without_a_name_or_places_is_rejected()
    {
        Assert.False(CustomBoardService.IsValid(new CustomBoardJson { name = "", locations = [new() { code = "a" }] }));
        Assert.False(CustomBoardService.IsValid(new CustomBoardJson { name = "Board", locations = null! }));
        Assert.False(CustomBoardService.IsValid(new CustomBoardJson { name = "Board", locations = [] }));
    }

    private static CustomBoardService CreateService() =>
        new(Mock.Of<ICustomBoardRepository>(), Mock.Of<ICustomLocationDataRepository>(), new CryptoService());

    [Fact]
    public async Task Oversized_files_are_refused_before_they_are_read_whole()
    {
        var huge = new MemoryStream(new byte[CustomBoardFile.MaxBytes + 1]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().ReadBoardJsonAsync(huge, "board.json"));
    }

    [Fact]
    public async Task Broken_json_is_reported_as_an_invalid_file()
    {
        var broken = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().ReadBoardJsonAsync(broken, "board.json"));
    }

    [Fact]
    public async Task Tile_sizes_are_kept_within_bounds()
    {
        var json = """{"name":"B","url":"https://x/{code}.jpg","width":-5,"height":999999,"locations":[{"code":"a"}]}""";

        var board = await CreateService().ReadBoardJsonAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), "board.json");

        Assert.Equal(0, board.width);
        Assert.Equal(4096, board.height);
    }
}
