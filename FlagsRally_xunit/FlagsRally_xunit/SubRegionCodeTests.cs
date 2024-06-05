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
        public void SubRegionCode_with_supported_country(string value)
        {
            SubRegionCode subRegionCode = new SubRegionCode(value);

            // Assert
            Assert.Equal(value.ToLower(), subRegionCode.lowerCountryCodeHyphenSubRegionCode);
        }

        [Fact]
        public void SubRegionCode_with_supported_country_and_subRegionCode()
        {
            // Arrange
            string countryCode = "JP";
            string subRegionCode = "01";

            // Act
            SubRegionCode subRegion = new SubRegionCode(countryCode, subRegionCode);

            // Assert
            Assert.NotNull(subRegion);
            Assert.Equal("JP-01".ToLower(), subRegion.lowerCountryCodeHyphenSubRegionCode);
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
        [InlineData("AB", "CD")]
        public void SubRegionCode_with_invalid_format_or_unsupported_country(string countryCode, string subRegionCode)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SubRegionCode(countryCode, subRegionCode));
        }
    }
}
