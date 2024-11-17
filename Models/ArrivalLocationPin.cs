using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

public class ArrivalLocationPin : Pin
{
    public ArrivalLocationPin(int id, DateTime dateTime, Location location)
        : this(id, dateTime, new Position(location.Latitude, location.Longitude))
    {
    }

    public ArrivalLocationPin(int id, DateTime dateTime, Position position)
    {
        Label = dateTime.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = position;
        Type = PinType.SavedPin;
        Tag = new MapPinTag(id);
    }
}
