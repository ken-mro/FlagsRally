using Maui.GoogleMaps;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin : Pin
{
    public CustomLocationPin(string key, string boardName, string title, bool isVisited, Position position)
    {
        Label = title;
        Position = position;
        Icon = SetIcon(isVisited);
        Type = PinType.Place;
        Tag = new MapPinTag(key, boardName, isVisited);
    }

    public static BitmapDescriptor SetIcon(bool isVisited)
    {
        var icon = isVisited ? "pin_arrived.png" : "pin.png";

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
