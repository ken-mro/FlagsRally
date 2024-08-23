using FlagsRally.Models;
using System;
using Xunit;

namespace FlagsRally.Tests.Models
{
    public class SubRegionCodeTests
    {
        [Theory]
        [InlineData("US-AL")]
        [InlineData("JP-01")]
        [InlineData("DE-BW")]
        [InlineData("IT-21")]
        [InlineData("FR-IDF")]
        [InlineData("MY-01")]
        [InlineData("NL-NH")]
        [InlineData("CA-AB")]
        [InlineData("NO-50")]
        [InlineData("CH-GE")]
        [InlineData("PT-11")]
        public void SubRegionCode_with_supported_country(string countrySubRegionCode)
        {
            SubRegionCode subRegionCode = new SubRegionCode(countrySubRegionCode);
            var test = countrySubRegionCode[..2];
            var test2 = countrySubRegionCode.Substring(3);

            // Assert
            Assert.NotNull(subRegionCode);
            Assert.Equal(countrySubRegionCode[..2], subRegionCode.CountryCode);
            Assert.Equal(countrySubRegionCode.Substring(3), subRegionCode.RegionCode);
            Assert.Equal(countrySubRegionCode.ToLower(), subRegionCode.lowerCountryCodeHyphenRegionCode);
            Assert.Equal(countrySubRegionCode.Replace("-", "_").ToLower(), subRegionCode.ImageResourceString);
        }

        [Theory]
        [InlineData("US","AL")]
        [InlineData("JP","01")]
        [InlineData("DE","BW")]
        [InlineData("IT","21")]
        [InlineData("FR","IDF")]
        [InlineData("MY","01")]
        [InlineData("NL","NH")]
        [InlineData("CA","AB")]
        [InlineData("NO", "50")]
        [InlineData("CH", "GE")]
        [InlineData("PT", "11")]
        public void SubRegionCode_with_supported_country_and_subRegionCode(string countryCode, string regionCode)
        {
            // Act
            SubRegionCode subRegionCode = new SubRegionCode(countryCode, regionCode);

            // Assert
            Assert.NotNull(subRegionCode);
            Assert.Equal(countryCode, subRegionCode.CountryCode);
            Assert.Equal(regionCode, subRegionCode.RegionCode);
            Assert.Equal($"{countryCode.ToLower()}-{regionCode.ToLower()}", subRegionCode.lowerCountryCodeHyphenRegionCode);
            Assert.Equal($"{countryCode.ToLower()}_{regionCode.ToLower()}", subRegionCode.ImageResourceString);
        }

        [Theory]
        [InlineData("AB-CD")]
        [InlineData("JP001")]
        public void SubRegionCode_with_invalid_format_or_country_code(string value)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SubRegionCode(value));
        }

        [Theory]
        [InlineData("GB", "ENG")]
        [InlineData("AB", "CD")]
        public void SubRegionCode_with_invalid_format_or_unsupported_country(string countryCode, string subRegionCode)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SubRegionCode(countryCode, subRegionCode));
        }
    }
}
