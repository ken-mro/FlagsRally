using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using Maui.GoogleMaps;
using System.Text.RegularExpressions;

namespace FlagsRally.Services;

public class CustomBoardService
{
    private readonly ICustomBoardRepository _customBoardRepository;
    private readonly ICustomLocationDataRepository _customLocationDataRepository;
    public CustomBoardService(ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository)
    {
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
    }


    //temp code
    static int i = 1;

    public CustomBoard GetCustomBoard(CustomBoardJson json)
    {
        return new CustomBoard()
        {
            Id = i++, //temp code
            Name = json.name,
            Url = json.url,
            Width = json.width,
            Height = json.height
        };
    }

    private int locationId = 1; //temp
    public List<CustomLocation> GetCustomLocations(CustomBoardJson json)
    {
        var customBoard = GetCustomBoard(json);
        var locations = new List<CustomLocation>();
        foreach (var location in json.locations)
        {
            locations.Add(new CustomLocation
            (
                id: locationId++, //temp value
                board: customBoard,
                code: location.code,
                title: location.title,
                subtitle: location.subtitle,
                group: location.group,
                //ImageUrl = ReplaceUrlPlaceholders(customBoard.Url, location),
                location: new Location()
                {
                    Latitude = location.latitude,
                    Longitude = location.longtitude
                },
                arrivalDate: DateTime.Now.Ticks % 2 == 0 ? DateTime.Now : null //temp value
            ));
        }
        return locations;
    }

    public (CustomBoard, List<CustomLocationPin>) GetCustomLocationPins(CustomBoardJson json)
    {
        var customBoard = GetCustomBoard(json);
        var pins = new List<CustomLocationPin>();
        int i = 0;
        foreach (var location in json.locations)
        {
            var position = new Position(location.latitude, location.longtitude);
            i++;
            pins.Add(new CustomLocationPin(i, customBoard.Id, location.title ?? string.Empty, i % 3 == 0, position));
        }
        return (customBoard, pins);
    }

    private static string ReplaceUrlPlaceholders(string url, CustomBoardLocationJson location)
    {
        var matches = Regex.Matches(url, @"\{(\w+)\}");
        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value;
            var property = location.GetType().GetProperty(propertyName);
            var value = property?.GetValue(location)?.ToString();

            url = url.Replace($"{{{propertyName}}}", value);
        }
        return url;
    }
}
