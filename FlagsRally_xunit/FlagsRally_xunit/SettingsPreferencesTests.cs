using FlagsRally.Repository;
using Moq;

namespace FlagsRallyTests.Repository;

public class SettingsPreferencesTests
{
    [Fact]
    public void Set_2_letter_country_code()
    {
        // Arrange
        var defaultPreferencesMock = new Mock<IPreferences>();
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
        string twoLetterISORegionName = "US";

        // Act
        settingsPreferences.SetCountryOfResidence(twoLetterISORegionName);

        // Assert
        defaultPreferencesMock.Verify(p => p.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Set_country_code_which_is_not_2_letter_is_invalid()
    {
        // Arrange
        var defaultPreferencesMock = new Mock<IPreferences>();
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
        string threeLetterISORegionName = "USA";

        // Act
        var exception = Assert.Throws<ArgumentException>(() => settingsPreferences.SetCountryOfResidence(threeLetterISORegionName));

        // Assert
        Assert.Equal("Two-letter ISO region name must be 2 characters long", exception.Message);
    }

    [Fact]
    public void Set_a_string_which_is_not_countyr_code_is_invalid()
    {
        // Arrange
        var defaultPreferencesMock = new Mock<IPreferences>();
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
        string twoLetterISORegionName = "1A";

        // Act
        var exception = Assert.Throws<ArgumentException>(() => settingsPreferences.SetCountryOfResidence(twoLetterISORegionName));

        // Assert
        Assert.Equal("Two-letter ISO region name only has letters", exception.Message);
    }

    [Fact]
    public void Get_default_country_code_if_it_has_not_set_before()
    {
        // Arrange
        var defaultPreferencesMock = new Mock<IPreferences>();
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
        string defaultRegion = "US";
        defaultPreferencesMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(defaultRegion);
        // Act
        var result = settingsPreferences.GetCountryOfResidence();

        // Assert
        Assert.Equal(defaultRegion, result);
    }
}
