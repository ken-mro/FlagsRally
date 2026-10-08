using FlagsRally.Resources;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

public class SelectedLocationPin : Pin
{
    public SelectedLocationPin(Position position)
    {
        Position = position;
        Label = $"{AppResources.GetLocationHere}";
        Anchor = new Point(0.5, 1);
        Icon = SetIcon();
        Address = SetAddress(position);
        IsDraggable = true;
    }

    private static BitmapDescriptor SetIcon() => PinIcons.For(PinKind.CheckInSpot);

    private static string SetAddress(Position position)
    {
        return $"{Math.Round(position.Latitude, 6)}, {Math.Round(position.Longitude, 6)}";
    }

    public void UpdateLocation(Position position)
    {
        Address = SetAddress(position);
    }
}
