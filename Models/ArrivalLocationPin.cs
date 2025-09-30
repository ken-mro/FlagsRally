using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

public class ArrivalLocationPin : Pin
{
    public ArrivalLocationPin(ArrivalLocationData arrivalLocationData)
    {
        Label = arrivalLocationData.AdminAreaName;
        Address = arrivalLocationData.ArrivalDate.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = new Position(arrivalLocationData.Latitude, arrivalLocationData.Longitude);
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetArrivalLocationTag(arrivalLocationData.Id);
        Anchor = new Point(0.5, 1);
        Icon = SetIcon();
    }

    public int Id => ((MapPinTag)Tag).ArrivalLocationId;

    private static BitmapDescriptor SetIcon()
    {
        var icon = "default_pin";
        return BitmapDescriptorFactory.FromBundle(icon);
    }
}
