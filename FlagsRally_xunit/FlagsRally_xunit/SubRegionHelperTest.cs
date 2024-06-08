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
        [InlineData("JP-01", "北海道")]
        [InlineData("jp-01", "北海道")]
        [InlineData("jP-01", "北海道")]
        [InlineData("US-AL", "")]
        public void Get_local_subregion_name_by(string regionCode, string regionName)
        {
            var customCountryHelper = new CustomCountryHelper();
            var subRegionHelper = new SubRegionHelper(customCountryHelper);
            var subRegion = new SubRegionCode(regionCode);

            Assert.True(subRegionHelper.GetLocalSubregionName(subRegion) == regionName);
        }
    }
}
