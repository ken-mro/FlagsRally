using FlagsRally.Helpers;
using Maui.GoogleMaps;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin : AppPin
{
    public CustomLocationPin(CustomLocationData data)
    {
        
        Label = data.Title;
        Address = data.ArrivalDate is DateTime arrival ? StampDate.Format(arrival) : null;
        var position = new Position(data.Latitude, data.Longitude);
        Position = position;

        var isVisited = data.ArrivalDate is not null;
        Icon = SetIcon(isVisited);

        Type = PinType.Place;
        Tag = MapPinTag.SetCustomLocationTag(data.CompositeKey, data.BoardName, isVisited);
        Anchor = new Point(0.5, 1);
    }

    public string CustomLocationKey => ((MapPinTag)Tag).CustomLocationKey;

    public void UpdateVisitStatus(DateTime? date)
    {
        var isVisited = date is not null;
        Icon = SetIcon(isVisited);
        ((MapPinTag)Tag).IsVisited = isVisited;

        Address = isVisited && date is DateTime visited ? StampDate.Format(visited) : null;
    }

    public bool IsVisited => ((MapPinTag)Tag).IsVisited;

    private static BitmapDescriptor SetIcon(bool isVisited) => PinIcons.For(isVisited ? PinKind.Visited : PinKind.Unvisited);
}
