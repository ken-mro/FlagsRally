using FlagsRally.Repository;
using Moq;
using System.Globalization;
using Xunit;

namespace FlagsRally.Tests.Repository
{
    public class SettingsPreferencesTests
    {
        [Fact]
        public void SetCountryOrRegion_ValidInput_SetsCountryOrRegion()
        {
            // Arrange
            var defaultPreferencesMock = new Mock<IPreferences>();
            var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
            string twoLetterISORegionName = "US";

            // Act
            settingsPreferences.SetCountryOrRegion(twoLetterISORegionName);

            // Assert
            defaultPreferencesMock.Verify(p => p.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SetCountryOrRegion_InvalidInput_ThrowsArgumentExceptionWithMessage()
        {
            // Arrange
            var defaultPreferencesMock = new Mock<IPreferences>();
            var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
            string threeLetterISORegionName = "USA";

            // Act
            var exception = Assert.Throws<ArgumentException>(() => settingsPreferences.SetCountryOrRegion(threeLetterISORegionName));

            // Assert
            Assert.Equal("Two-letter ISO region name must be 2 characters long", exception.Message);
        }

        [Fact]
        public void SetCountryOrRegion_InvalidInput_ThrowsArgumentExceptionWithMessage2()
        {
            // Arrange
            var defaultPreferencesMock = new Mock<IPreferences>();
            var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
            string twoLetterISORegionName = "1A";

            // Act
            var exception = Assert.Throws<ArgumentException>(() => settingsPreferences.SetCountryOrRegion(twoLetterISORegionName));

            // Assert
            Assert.Equal("Two-letter ISO region name only has letters", exception.Message);
        }

        [Fact]
        public void GetCountryOrRegion_ReturnsDefaultRegion()
        {
            // Arrange
            var defaultPreferencesMock = new Mock<IPreferences>();
            var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
            string defaultRegion = "US";
            defaultPreferencesMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(defaultRegion);
            // Act
            var result = settingsPreferences.GetCountryOrRegion();

            // Assert
            Assert.Equal(defaultRegion, result);
        }
    }
}
