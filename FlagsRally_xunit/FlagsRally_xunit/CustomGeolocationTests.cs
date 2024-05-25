using FlagsRally.Utilities;

namespace FlagsRally.Models.Tests;

public class CustomGeolocationTests
{
    [Fact]
    public async Task Get_location_Info_from_location_coordinates()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode = "en";

        // Act
        Placemark placemark = await CustomGeolocation.GetPlacemarkAsync(location, languageCode);

        // Assert
        Assert.NotNull(placemark);
        Assert.Equal("United States", placemark.CountryName);
        Assert.Equal("US", placemark.CountryCode);
        Assert.Equal("California", placemark.AdminArea);
        Assert.Equal("San Francisco", placemark.Locality);
        Assert.Equal(location, placemark.Location);
    }

    [Fact]
    public async Task Get_location_Info_from_location_coordinates_in_other_language()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode = "ja";

        // Act
        Placemark placemark = await CustomGeolocation.GetPlacemarkAsync(location, languageCode);

        // Assert
        Assert.NotNull(placemark);
        Assert.Equal("アメリカ合衆国", placemark.CountryName);
        Assert.Equal("US", placemark.CountryCode);
        Assert.Equal("カリフォルニア州", placemark.AdminArea);
        Assert.Equal("サンフランシスコ", placemark.Locality);
        Assert.Equal(location, placemark.Location);
    }

    [Fact]
    public async Task Language_code_length_is_2()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode3 = "eng";
        string languageCode1 = "e";

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(() => CustomGeolocation.GetPlacemarkAsync(location, languageCode3));
        await Assert.ThrowsAsync<ArgumentException>(() => CustomGeolocation.GetPlacemarkAsync(location, languageCode1));
    }

    [Fact]
    public async Task Language_code_consists_of_letters()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode = "e1";

        await Assert.ThrowsAsync<ArgumentException>(() => CustomGeolocation.GetPlacemarkAsync(location, languageCode));
    }
}
