using FlagsRally.Models.CustomBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlagsRally.Services;

public class CustomBoardService
{
    public CustomBoardService()
    {
    }

    private CustomBoard GetCustomBoard(CustomBoardJson json)
    {
        return new CustomBoard()
        {
            Name = json.name,
            Url = json.url,
            Width = json.width,
            Height = json.height
        };
    }

    public List<CustomLocation> GetCustomLocations(CustomBoardJson json)
    {
        var customBoard = GetCustomBoard(json);
        var locations = new List<CustomLocation>();
        foreach (var location in json.locations)
        {
            locations.Add(new CustomLocation()
            {
                Board = customBoard,
                Code = location.code,
                Title = location.title,
                Subtitle = location.subtitle,
                Group = location.group,
                ImageUrl = ReplaceUrlPlaceholders(customBoard.Url, location),
                Location = new Location()
                {
                    Latitude = location.latitude,
                    Longitude = location.longtitude
                },
                ArrivalDate = DateTime.Now.Ticks % 2 == 0 ? DateTime.Now : null //temp value
            });
        }
        return locations;
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
