using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin : Pin
{
    public CustomLocationPin(CustomLocationData data)
    {
        
        Label = data.Title;
        Address = data.ArrivalDate?.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
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

        Address = isVisited ? date?.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US")) : null;
    }

    public bool IsVisited => ((MapPinTag)Tag).IsVisited;

    private static BitmapDescriptor SetIcon(bool isVisited)
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
