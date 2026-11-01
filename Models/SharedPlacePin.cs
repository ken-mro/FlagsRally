using FlagsRally.Resources;
using FlagsRally.Services;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

/// <summary>
/// A place shared from another map app, to see what is around it. Only on the map until it is removed
/// or another place is shared; it has no tag, so the filter never hides it.
/// </summary>
public class SharedPlacePin : AppPin
{
    public SharedPlacePin(SharedPlace place)
    {
        Label = place.Name ?? AppResources.SharedPlace;
        HasName = place.Name is not null;
        Address = $"{Math.Round(place.Location.Latitude, 6)}, {Math.Round(place.Location.Longitude, 6)}";
        Position = new Position(place.Location.Latitude, place.Location.Longitude);
        Anchor = new Point(0.5, 1);
        Icon = PinIcons.For(PinKind.SharedPlace);
    }

    // Without a name the label already says "Shared place"
    public bool HasName { get; }
}
