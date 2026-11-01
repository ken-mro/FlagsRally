using FlagsRally.Resources;
using Maui.GoogleMaps;
using System.Globalization;

namespace FlagsRally.Models;

/// <summary>
/// A stamp waiting for its country and region (PendingArrivalData), shown with the arrivals.
/// </summary>
public class PendingArrivalPin : AppPin
{
    public PendingArrivalPin(PendingArrivalData pendingArrival)
    {
        Label = AppResources.PendingStamp;
        Address = pendingArrival.ArrivalDate.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
        Position = new Position(pendingArrival.Latitude, pendingArrival.Longitude);
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetPendingArrivalTag(pendingArrival.Id);
        Anchor = new Point(0.5, 1);
        Icon = PinIcons.For(PinKind.Pending);
    }

    public int Id => ((MapPinTag)Tag).PendingArrivalId;
}
