using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

public class ArrivalLocationPin : Pin
{
    public ArrivalLocationPin(int id, DateTime dateTime, Location location)
    {
        var position = new Position(location.Latitude, location.Longitude);
        Initialize(id, dateTime, position);
    }

    public ArrivalLocationPin(int id, DateTime dateTime, Position position)
    {
        Initialize(id, dateTime, position);
    }

    private void Initialize(int id, DateTime dateTime, Position position)
    {
        Label = dateTime.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = position;
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetArrivalLocationTag(id);
        Anchor = new Point(0.5, 1);
        Icon = SetIcon();
    }

    public int Id => ((MapPinTag)Tag).ArrivalLocationId;

    private static BitmapDescriptor SetIcon()
    {
        var icon = "default_pin.png";

        var imageSource = () => new ContentView()
        {
            Content = new Image
            {
                Source = icon,
                WidthRequest = 50,
                HeightRequest = 50
            }
        };

        return BitmapDescriptorFactory.FromView(imageSource);
    }
}
