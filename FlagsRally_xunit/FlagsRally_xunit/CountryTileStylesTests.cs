using FlagsRally.Models;
using FlagsRally.Repository;

namespace FlagsRallyTests.Models;

public class CountryTileStylesTests
{
    [Fact]
    public void Every_country_with_regions_has_its_own_style()
    {
        foreach (var code in Constants.SupportedSubRegionCountryCodeList)
            Assert.NotSame(CountryTileStyles.Default, CountryTileStyles.For(code));
        Assert.Equal(Constants.SupportedSubRegionCountryCodeList.Count, CountryTileStyles.CountryCodes.Count);
    }

    [Fact]
    public void The_code_is_matched_in_either_case()
    {
        Assert.Same(CountryTileStyles.For("JP"), CountryTileStyles.For("jp"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GB")]
    public void Any_other_code_keeps_the_original_look(string? code)
    {
        Assert.Same(CountryTileStyles.Default, CountryTileStyles.For(code));
    }

    [Fact]
    public void A_region_takes_its_country_style()
    {
        var region = new SubRegion { Code = new SubRegionCode("fr-idf") };
        Assert.Equal("FR", region.Tile.CountryCode);
        Assert.NotEqual(Colors.Transparent, region.Tile.BandLine);
    }

    [Fact]
    public void Only_the_squared_tiles_have_small_corners()
    {
        Assert.Equal(6, CountryTileStyles.For("CH").Radius);
        Assert.Equal(20, CountryTileStyles.For("JP").Radius);
    }
}
