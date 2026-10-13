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
    public void Set_a_string_which_is_not_country_code_is_invalid()
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
    public void Get_the_chosen_country_code()
    {
        // Arrange
        var defaultPreferencesMock = new Mock<IPreferences>();
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);
        defaultPreferencesMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("JP");
        // Act
        var result = settingsPreferences.GetCountryOfResidence();

        // Assert
        Assert.Equal("JP", result);
    }

    [Fact]
    public void A_country_code_is_found_when_none_has_been_chosen()
    {
        var defaultPreferencesMock = new Mock<IPreferences>();
        defaultPreferencesMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((string _, string d, string _) => d);
        var settingsPreferences = new SettingsPreferences(defaultPreferencesMock.Object);

        var result = settingsPreferences.GetCountryOfResidence();

        Assert.Matches("^[A-Z]{2}$", result);
        Assert.NotEqual("IV", result);
    }

    [Theory]
    [InlineData("JP", "IV", "JP", "US")]
    [InlineData("JP", "", "jp", "US")]
    [InlineData("US", "001", null, "US")]
    [InlineData("DE", "ZZ", "de", "US")]
    public void The_first_real_country_code_is_used(string expected, string? first, string? second, string? last)
    {
        Assert.Equal(expected, SettingsPreferences.PickCountryCode(first, second, last));
    }
}
