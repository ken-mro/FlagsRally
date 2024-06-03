using CountryData.Standard;

namespace FlagsRally_xunit;

public class CountryHelperTest
{
    [Fact]
    public void JA_SubregionCode()
    {
        //Arrange
        var countryHelper = new CountryHelper();

        //Act
        var regionList = countryHelper?.GetCountryByCode("JP").Regions.ToList();

        //Assrt
        Assert.True(regionList?.Count == 47);
    }

    [Fact]
    public void US_SubregionCode()
    {
        //Arrange
        var countryHelper = new CountryHelper();

        //Act
        var regionList = countryHelper?.GetCountryByCode("US").Regions
                        .Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }
                        .Contains(x.ShortCode)).ToList();

        //Assrt
        Assert.True(regionList?.Count == 51);
    }

    [Fact]
    public void DE_SubregionCode()
    {
        //Arrange
        var countryHelper = new CountryHelper();

        //Act
        var regionList = countryHelper?.GetCountryByCode("DE").Regions.ToList();

        //Assrt
        Assert.True(regionList?.Count == 16);
    }

    [Fact]
    public void FR_SubregionCode()
    {
        //Arrange
        var countryHelper = new CountryHelper();

        //Act
        var regionList = countryHelper?.GetCountryByCode("FR").Regions.ToList();

        //Assrt
        Assert.True(regionList?.Count == 26);
    }

    [Fact]
    public void IT_SubregionCode()
    {
        //Arrange
        var countryHelper = new CountryHelper();

        //Act
        var regionList = countryHelper?.GetCountryByCode("IT").Regions.ToList();

        //Assrt
        Assert.True(regionList?.Count == 20);
    }
}
