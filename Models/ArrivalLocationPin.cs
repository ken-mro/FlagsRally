using FlagsRally.Helpers;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

public class ArrivalLocationPin : AppPin
{
    public ArrivalLocationPin(ArrivalLocationData arrivalLocationData)
    {
        Label = arrivalLocationData.AdminAreaName;
        Address = StampDate.Format(arrivalLocationData.ArrivalDate);
        Position = new Position(arrivalLocationData.Latitude, arrivalLocationData.Longitude);
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetArrivalLocationTag(arrivalLocationData.Id);
        Anchor = new Point(0.5, 1);
        Icon = SetIcon();
    }

    public ArrivalLocationPin(ArrivalLocation arrivalLocation)
    {
        Label = arrivalLocation.AdminAreaName;
        Address = StampDate.Format(arrivalLocation.ArrivalDate);
        Position = new Position(arrivalLocation.Location.Latitude, arrivalLocation.Location.Longitude);
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetArrivalLocationTag(arrivalLocation.Id);
        Anchor = new Point(0.5, 1);
        Icon = SetIcon();
    }

    public int Id => ((MapPinTag)Tag).ArrivalLocationId;

    private static BitmapDescriptor SetIcon() => PinIcons.For(PinKind.CheckIn);
}
