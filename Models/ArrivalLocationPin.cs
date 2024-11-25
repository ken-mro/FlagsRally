using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

public class ArrivalLocationPin : Pin
{
    public ArrivalLocationPin(DateTime dateTime, Location location)
    {
        var position = new Position(location.Latitude, location.Longitude);
        Initialize(dateTime, position);
    }

    public ArrivalLocationPin(DateTime dateTime, Position position)
    {
        Initialize(dateTime, position);
    }

    private void Initialize(DateTime dateTime, Position position)
    {
        Label = dateTime.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = position;
        Type = PinType.SavedPin;
        Tag = new MapPinTag();
        Icon = SetIcon();
    }

    private static BitmapDescriptor SetIcon()
    {
        var icon = "default_pin.png";

        var imageSource = () => new ContentView()
        {
            Content = new Image
            {
                Source = icon,
                WidthRequest = 40,
                HeightRequest = 40
            }
        };

        return BitmapDescriptorFactory.FromView(imageSource);
    }
}
