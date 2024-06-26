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
        var customGeolocation = new CustomGeolocation(new CustomCountryHelper());
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
        var customGeolocation = new CustomGeolocation(new CustomCountryHelper());
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
    public async Task Generate_location_data_from_location_coordinates_in_other_2()
    {
        // Arrange
        Location location = new Location(12.984305, -61.287228);
        string languageCode = "ja";
        var customGeolocation = new CustomGeolocation(new CustomCountryHelper());
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
    public async Task Generate_location_data_from_location_coordinates_in_other_language_3()
    {
        // Arrange
        Location location = new Location(18.0410885197085, -63.05616148029151);
        string languageCode = "ja";
        var customGeolocation = new CustomGeolocation(new CustomCountryHelper());
        var datetime = DateTime.Now;

        // Act
        var arrivalLocationData = await customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

        // Assert
        Assert.NotNull(arrivalLocationData);
        Assert.Equal(datetime, arrivalLocationData.ArrivalDate);
        Assert.Equal("シント・マールテン", arrivalLocationData.CountryName);
        Assert.Equal("シント マールテン島", arrivalLocationData.AdminAreaName);
        Assert.Equal("フィリップスバーグ", arrivalLocationData.LocalityName);

        Assert.Equal("Sint Maarten", arrivalLocationData.EnCountryName);
        Assert.Equal("Sint Maarten", arrivalLocationData.EnAdminAreaName);
        Assert.Equal("Philipsburg", arrivalLocationData.EnLocalityName);

        Assert.Equal("SX", arrivalLocationData.CountryCode);
        Assert.Equal("", arrivalLocationData.AdminAreaCode);
        Assert.Equal(location.Longitude, arrivalLocationData.Longitude);
        Assert.Equal(location.Latitude, arrivalLocationData.Latitude);
    }
}
