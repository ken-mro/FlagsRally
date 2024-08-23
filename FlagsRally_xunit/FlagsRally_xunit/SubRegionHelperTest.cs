using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Utilities;


namespace FlagsRally_xunit
{
    public class SubRegionHelperTest
    {
        [Theory]
        [InlineData("JP", true)]
        [InlineData("jp", true)]
        [InlineData("jP", true)]
        [InlineData("US", false)]
        public void Validate_a_country_is_Supported(string countryCode, bool isSupported)
        {
            var customCountryHelper = new CustomCountryHelper();
            var subRegionHelper = new SubRegionHelper(customCountryHelper);

            Assert.True(subRegionHelper.GetLocalSubRegionNameIsSupported(countryCode) == isSupported);
        }

        [Theory]
        [InlineData("JP", 47, "JP-01", "北海道")]
        [InlineData("US", 51, "US-AL", "Alabama")]
        [InlineData("DE", 16, "DE-BW", "Baden-Württemberg")]
        [InlineData("FR", 13, "FR-IDF", "Île-de-France")]
        [InlineData("IT", 20, "IT-21", "Lombardia")]
        [InlineData("NL", 12, "NL-NH", "Noord-Holland")]
        [InlineData("MY", 16, "MY-01", "Johor")]
        [InlineData("CA", 13, "CA-AB", "Alberta")]
        [InlineData("NO", 15, "NO-50", "Trøndelag")]
        [InlineData("CH", 26, "CH-GE", "Genève")]
        [InlineData("PT", 20, "PT-11", "Lisboa")]
        public void Get_blank_all_reagion_list_with_local_name_if_supported(string regionCode, int subRegionCount, string countryHyphenRegionCode, string localName)
        {
            // Arrange
            var countryHelper = new CustomCountryHelper();
            var countryInfo = countryHelper.GetCountryByCode(regionCode);
            var subRegionHelper = new SubRegionHelper(countryHelper);

            // Act
            var blankAllRegionList = subRegionHelper.GetBlankAllRegionList(countryInfo, "JP");

            // Assert
            Assert.True(blankAllRegionList.Count == subRegionCount);
            Assert.Contains(blankAllRegionList, x => x.Code.lowerCountryCodeHyphenRegionCode.Equals(countryHyphenRegionCode.ToLower()));
            Assert.Contains(blankAllRegionList, x => x.Name.Equals(localName));
        }
    }
}
