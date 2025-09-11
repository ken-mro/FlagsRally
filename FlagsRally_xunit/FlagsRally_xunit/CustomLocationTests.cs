using FlagsRally.Models.CustomBoard;
using System.Globalization;

namespace FlagsRallyTests.Models;

public class CustomLocationTests
{
    [Fact]
    public void Code_can_be_embeded_in_the_url()
    {
        // Arrange
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name= "Test Board", Url= "https://www.gk-p.jp/wp-content/uploads/mhc/{code}.jpg", Width= 100, Height= 200 },
            code : "01-204-B-01",
            title : "àÆêÏés",
            subtitle : "ñkäCìπ",
            group : "ñkäCìπ",
            location : new Location(43.762278, 142.359194),
            arrivalDate: DateTime.Now
        );

        // Act
        var imageUrl = customLocation.ImageUrl;

        // Assert
        Assert.NotNull(imageUrl);
        Assert.True(imageUrl?.Equals("https://www.gk-p.jp/wp-content/uploads/mhc/01-204-B-01.jpg"));
    }

    [Fact]
    public void All_the_string_properties_can_be_embeded_in_the_url()
    {
        // Arrange
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name = "Test Board", Url = "https://www.sample/{title}/{subtitle}/{group}/{code}.jpg", Width = 100, Height = 200 },
            code: "29-205-B-01",
            title: "kashihara-shi",
            subtitle: "nara-ken",
            group: "kinki",
            location: new Location(43.762278, 142.359194),
            arrivalDate: DateTime.Now
        );

        // Act
        var imageUrl = customLocation.ImageUrl;

        // Assert
        Assert.NotNull(imageUrl);
        Assert.True(imageUrl?.Equals("https://www.sample/kashihara-shi/nara-ken/kinki/29-205-B-01.jpg"));
    }

    [Fact]
    public void The_location_has_visited()
    {
        // Arrange
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name = "Test Board", Url = "https://www.gk-p.jp/wp-content/uploads/mhc/{code}.jpg", Width = 100, Height = 200 },
            code: "01-204-B-01",
            title: "àÆêÏés",
            subtitle: "ñkäCìπ",
            group: "ñkäCìπ",
            location: new Location(43.762278, 142.359194),
            arrivalDate: DateTime.Now
        );

        // Act
        var hasBeenVisited = customLocation.HasBeenVisited;
        var hasNotBeenVisited = customLocation.HasNotBeenVisited;

        // Assert
        Assert.True(hasBeenVisited);
        Assert.False(hasNotBeenVisited);
    }

    [Fact]
    public void The_location_has_not_visited()
    {
        // Arrange
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name = "Test Board", Url = "https://www.gk-p.jp/wp-content/uploads/mhc/{code}.jpg", Width = 100, Height = 200 },
            code: "01-204-B-01",
            title: "àÆêÏés",
            subtitle: "ñkäCìπ",
            group: "ñkäCìπ",
            location: new Location(43.762278, 142.359194),
            arrivalDate: null
        );

        // Act
        var hasBeenVisited = customLocation.HasBeenVisited;
        var hasNotBeenVisited = customLocation.HasNotBeenVisited;


        // Assert
        Assert.False(hasBeenVisited);
        Assert.True(hasNotBeenVisited);
    }

    [Fact]
    public void Show_visted_date_in_specific_format()
    {
        // Arrange
        // Act
        var arrivalDate = new DateTime(2023, 10, 1);
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name = "Test Board", Url = "https://www.gk-p.jp/wp-content/uploads/mhc/{code}.jpg", Width = 100, Height = 200 },
            code: "01-204-B-01",
            title: "àÆêÏés",
            subtitle: "ñkäCìπ",
            group: "ñkäCìπ",
            location: new Location(43.762278, 142.359194),
            arrivalDate: arrivalDate
        );

        // Assert
        Assert.Equal(arrivalDate.ToString("dd  MMM  yyyy", CultureInfo.CreateSpecificCulture("en-US")), customLocation.ArrivalDateString);
    }

    [Fact]
    public void Nothing_is_shown_for_the_date_if_the_location_is_unvisited()
    {
        // Arrange
        var customLocation = new CustomLocation
        (
            board: new CustomBoard { Name = "Test Board", Url = "https://www.gk-p.jp/wp-content/uploads/mhc/{code}.jpg", Width = 100, Height = 200 },
            code: "01-204-B-01",
            title: "àÆêÏés",
            subtitle: "ñkäCìπ",
            group: "ñkäCìπ",
            location: new Location(43.762278, 142.359194),
            arrivalDate: null
        );

        // Act
        var arrivalDateString = customLocation.ArrivalDateString;

        // Assert
        Assert.Equal(string.Empty, arrivalDateString);
    }
}
