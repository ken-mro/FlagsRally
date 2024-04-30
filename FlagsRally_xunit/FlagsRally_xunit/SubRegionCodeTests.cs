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
            Assert.Equal(value, subRegionCode.Value);
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
    }
}
