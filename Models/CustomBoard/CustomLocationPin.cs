using Maui.GoogleMaps;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin : Pin
{
    public CustomLocationPin(int id, int boardId, string title, bool isVisited, Position position)
    {
        Label = title;
        Position = position;

        var color = isVisited ? Color.FromArgb("#00552E") : Color.FromArgb("#1E50A2");
        Icon = BitmapDescriptorFactory.DefaultMarker(color);

        Type = PinType.Place;
        Tag = new MapPinTag(id, boardId);
    }
}
