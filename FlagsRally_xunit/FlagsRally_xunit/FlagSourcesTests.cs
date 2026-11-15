using FlagsRally.Helpers;
using FlagsRally.Models;

namespace FlagsRallyTests.Helpers;

public class FlagSourcesTests
{
    [Theory]
    [InlineData("JP", "https://flagcdn.com/160x120/jp.png")]
    [InlineData("us-ca", "https://flagcdn.com/160x120/us-ca.png")]
    public void Countries_and_US_states_come_from_flagcdn(string code, string expected) =>
        Assert.Equal(expected, FlagSources.FlagCdnUrl(code));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void US_states_have_the_same_flag_with_or_without_emblem(bool emblem)
    {
        Assert.Equal("https://flagcdn.com/160x120/us-ny.png", FlagSources.RegionFlag(new SubRegionCode("US", "NY"), emblem));
        Assert.Equal("us_dc.png", FlagSources.RegionFlag(new SubRegionCode("US", "DC"), emblem));
    }

    [Fact]
    public void Other_regions_use_the_bundled_flag_or_emblem()
    {
        var kyoto = new SubRegionCode("JP", "26");
        Assert.Equal("jp_26.png", FlagSources.RegionFlag(kyoto));
        Assert.Equal("jp_26_emblem.png", FlagSources.RegionFlag(kyoto, emblem: true));
    }

    // France is one of the countries with regional flags
    [Fact]
    public void French_regions_have_their_own_flag() =>
        Assert.Equal("fr_idf.png", FlagSources.RegionFlag(new SubRegionCode("FR", "IDF")));

    [Fact]
    public void A_country_without_regions_is_refused() =>
        Assert.Throws<ArgumentException>(() => FlagSources.RegionFlag(new SubRegionCode("GB", "ENG")));
}
