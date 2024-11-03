using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

public class ArrivalLocationPin : Pin
{
    public int Id { get; set; }
    public DateTime ArrivalDate { get; set; }

    public ArrivalLocationPin(int id, DateTime dateTime, Location location)
        : this(id, dateTime, new Position(location.Latitude, location.Longitude))
    {
    }

    public ArrivalLocationPin(int id, DateTime dateTime, Position position)
    {
        Id = id;
        ArrivalDate = dateTime;
        Label = ArrivalDate.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = position;
        Type = PinType.Place;
    }
}
