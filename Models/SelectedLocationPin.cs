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

    private static BitmapDescriptor SetIcon()
    {
        var icon = "selected_location_pin.png";

        var imageSource = () => new ContentView()
        {
            Content = new Image
            {
                Source = icon,
                WidthRequest = 80,
                HeightRequest = 80
            }
        };

        return BitmapDescriptorFactory.FromView(imageSource);
    }

    private static string SetAddress(Position position)
    {
        return $"{Math.Round(position.Latitude, 3)}, {Math.Round(position.Longitude, 3)}";
    }

    public void UpdateLocation(Position position)
    {
        Address = SetAddress(position);
    }
}
