using Maui.GoogleMaps;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin : Pin
{
    public CustomLocationPin(string key, string boardName, string title, bool isVisited, Position position)
    {
        Label = title;
        Position = position;
        Icon = CustomLocationPin.SetIcon(isVisited);
        Type = PinType.Place;
        Tag = MapPinTag.SetCustomLocationTag(key, boardName, isVisited);
        Anchor = new Point(0.5, 1);
    }

    public string CustomLocationKey => ((MapPinTag)Tag).CustomLocationKey;

    public void UpdateVisitStatus(bool isVisited)
    {
        Icon = SetIcon(isVisited);
        ((MapPinTag)Tag).IsVisited = isVisited;
    }

    public bool IsVisited => ((MapPinTag)Tag).IsVisited;

    public static BitmapDescriptor SetIcon(bool isVisited)
    {
        var icon = isVisited ? "pin_arrived.png" : "pin.png";

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
