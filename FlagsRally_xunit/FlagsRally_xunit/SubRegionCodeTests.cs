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
        public void SubRegionCode_WithValidValue_ShouldSetPropertyValue(string value)
        {
            SubRegionCode subRegionCode = new SubRegionCode(value);

            // Assert
            Assert.Equal(value.ToLower(), subRegionCode.lower5LetterRegionCode);
        }

        [Theory]
        [InlineData("AB-CD")]
        [InlineData("JP-001")]
        [InlineData("JP001")]

        [InlineData("JP-AL")]
        [InlineData("JP-A1")]
        [InlineData("JP-1A")]

        [InlineData("US-A1")]
        [InlineData("US-1A")]
        [InlineData("US-01")]
        public void SubRegionCode_WithInvalidValue_ShouldThrowArgumentException(string value)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SubRegionCode(value));
        }

        [Fact]
        public void SubRegionCode_WithValidCodes_ShouldCreateInstance()
        {
            // Arrange
            string countryCode = "JP";
            string subRegionCode = "01";

            // Act
            SubRegionCode subRegion = new SubRegionCode(countryCode, subRegionCode);

            // Assert
            Assert.NotNull(subRegion);
            Assert.Equal("JP-01".ToLower(), subRegion.lower5LetterRegionCode);
        }

        [Theory]
        [InlineData("AB", "CD")]
        [InlineData("JP","001")]
        [InlineData("JP","AL")]
        [InlineData("JP","A1")]
        [InlineData("JP","1A")]
        [InlineData("US","A1")]
        [InlineData("US","1A")]
        [InlineData("US","01")]
        public void SubRegionCode_WithInvalidCountryCode_ShouldThrowArgumentException(string countryCode, string subRegionCode)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SubRegionCode(countryCode, subRegionCode));
        }
    }
}
