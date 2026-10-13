using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class BoardSeriesTests
{
    [Theory]
    [InlineData("マンホールカード第01弾", "マンホールカード")]
    [InlineData("マンホールカード第23弾-1", "マンホールカード")]
    [InlineData("マンホールカード特別版（東京都）", "マンホールカード")]
    [InlineData("マンホールカード特別版第2弾（東京都）", "マンホールカード")]
    [InlineData("Shrines Vol. 2", "Shrines")]
    [InlineData("Castles part 3", "Castles")]
    [InlineData("Stations-4", "Stations")]
    [InlineData("全国一の宮巡り", "全国一の宮巡り")]
    [InlineData("全国神宮二十五社巡り", "全国神宮二十五社巡り")]
    [InlineData("2025年大阪万博各国パビリオンスタンプ", "2025年大阪万博各国パビリオンスタンプ")]
    [InlineData("88", "88")]
    public void A_name_without_its_issue_number_is_its_series(string name, string series)
    {
        Assert.Equal(series, BoardSeries.FromName(name));
    }

    [Fact]
    public void The_folder_a_board_came_from_is_its_series()
    {
        Assert.Equal("Expo25", BoardSeries.Of("Expo_2025_Osaka_Country_Pavilion_Stamps", "Expo25"));
        Assert.Equal("マンホールカード", BoardSeries.Of("マンホールカード第05弾", null));
        Assert.Equal("マンホールカード", BoardSeries.Of("マンホールカード第05弾", " "));
    }

    [Fact]
    public void A_subfolder_is_named_with_its_parents()
    {
        Assert.Equal("マンホールカード／関東", BoardSeries.FolderPath(["マンホールカード", "関東"]));
        Assert.Equal("Expo25", BoardSeries.FolderPath(["Expo25"]));
        Assert.Equal("", BoardSeries.FolderPath([]));
    }

    [Fact]
    public void Subfolders_with_the_same_name_in_different_series_stay_apart()
    {
        var manhole = BoardSeries.Of("Board A", BoardSeries.FolderPath(["マンホールカード", "関東"]));
        var shrines = BoardSeries.Of("Board B", BoardSeries.FolderPath(["神社", "関東"]));

        Assert.NotEqual(manhole, shrines);
    }
}
