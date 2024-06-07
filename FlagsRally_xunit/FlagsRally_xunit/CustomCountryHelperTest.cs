using FlagsRally.Utilities;

namespace FlagsRally_xunit;

public class CustomCountryHelperTest
{
    [Theory]
    [InlineData("JP", 47)]
    [InlineData("US", 51)]
    [InlineData("DE", 16)]
    [InlineData("FR", 13)]
    [InlineData("IT", 20)]
    [InlineData("NL", 12)]
    [InlineData("MY", 16)]
    public void SubregionCode(string CountryCode, int regionCount)
    {
        //Arrange
        var countryHelper = new CustomCountryHelper();

        //Act
        var regionList = countryHelper?.GetDistinctCountryRegionsBy(CountryCode).ToList();

        //Assert
        Assert.True(regionList?.Count == regionCount);
    }
}
