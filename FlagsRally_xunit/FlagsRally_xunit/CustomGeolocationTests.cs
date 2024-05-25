using FlagsRally.Utilities;

namespace FlagsRally.Utilities.Tests;

public class CustomGeolocationTests
{
    [Fact]
    public async Task Generate_location_data_from_location_coordinates()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode = "en";
        var customGeolocation = new CustomGeolocation(new CountryData.Standard.CountryHelper());
        var datetime = DateTime.Now;

        // Act
        var arrivalLocationData = await customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

        // Assert
        Assert.NotNull(arrivalLocationData);
        Assert.Equal(datetime, arrivalLocationData.ArrivalDate);
        Assert.Equal("United States", arrivalLocationData.CountryName);
        Assert.Equal("California", arrivalLocationData.AdminAreaName);
        Assert.Equal("San Francisco", arrivalLocationData.LocalityName);

        Assert.Equal("United States", arrivalLocationData.EnCountryName);
        Assert.Equal("California", arrivalLocationData.EnAdminAreaName);
        Assert.Equal("San Francisco", arrivalLocationData.EnLocalityName);

        Assert.Equal("US", arrivalLocationData.CountryCode);
        Assert.Equal("CA", arrivalLocationData.AdminAreaCode);
        Assert.Equal(location.Longitude, arrivalLocationData.Longitude);
        Assert.Equal(location.Latitude, arrivalLocationData.Latitude);
    }

    [Fact]
    public async Task Generate_location_data_from_location_coordinates_in_other_language()
    {
        // Arrange
        Location location = new Location(37.7749, -122.4194);
        string languageCode = "ja";
        var customGeolocation = new CustomGeolocation(new CountryData.Standard.CountryHelper());
        var datetime = DateTime.Now;

        // Act
        var arrivalLocationData = await customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

        // Assert
        Assert.NotNull(arrivalLocationData);
        Assert.Equal(datetime, arrivalLocationData.ArrivalDate);
        Assert.Equal("アメリカ合衆国", arrivalLocationData.CountryName);
        Assert.Equal("カリフォルニア州", arrivalLocationData.AdminAreaName);
        Assert.Equal("サンフランシスコ", arrivalLocationData.LocalityName);

        Assert.Equal("United States", arrivalLocationData.EnCountryName);
        Assert.Equal("California", arrivalLocationData.EnAdminAreaName);
        Assert.Equal("San Francisco", arrivalLocationData.EnLocalityName);

        Assert.Equal("US", arrivalLocationData.CountryCode);
        Assert.Equal("CA", arrivalLocationData.AdminAreaCode);
        Assert.Equal(location.Longitude, arrivalLocationData.Longitude);
        Assert.Equal(location.Latitude, arrivalLocationData.Latitude);
    }

    [Fact]
    public async Task Generate_location_data_from_location_coordinates_in_other_of_the_2_language()
    {
        // Arrange
        Location location = new Location(12.984305, -61.287228);
        string languageCode = "ja";
        var customGeolocation = new CustomGeolocation(new CountryData.Standard.CountryHelper());
        var datetime = DateTime.Now;

        // Act
        var arrivalLocationData = await customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

        // Assert
        Assert.NotNull(arrivalLocationData);
        Assert.Equal(datetime, arrivalLocationData.ArrivalDate);
        Assert.Equal("セントビンセントおよびグレナディーン諸島", arrivalLocationData.CountryName);
        Assert.Equal("Grenadines", arrivalLocationData.AdminAreaName);
        Assert.Equal("ポート・エリザベス", arrivalLocationData.LocalityName);

        Assert.Equal("Saint Vincent and the Grenadines", arrivalLocationData.EnCountryName);
        Assert.Equal("Grenadines", arrivalLocationData.EnAdminAreaName);
        Assert.Equal("Port Elizabeth", arrivalLocationData.EnLocalityName);

        Assert.Equal("VC", arrivalLocationData.CountryCode);
        Assert.Equal("06", arrivalLocationData.AdminAreaCode);
        Assert.Equal(location.Longitude, arrivalLocationData.Longitude);
        Assert.Equal(location.Latitude, arrivalLocationData.Latitude);
    }

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
