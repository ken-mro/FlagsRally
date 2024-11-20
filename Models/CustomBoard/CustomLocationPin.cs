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
        var color = isVisited ? Color.FromArgb("#00552E") : Color.FromArgb("#1E50A2");
        return BitmapDescriptorFactory.DefaultMarker(color);
    }
}
