using FlagsRally.Helpers;
using FlagsRally.Resources;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

/// <summary>
/// A stamp waiting for its country and region (PendingArrivalData), shown with the arrivals.
/// </summary>
public class PendingArrivalPin : AppPin
{
    public PendingArrivalPin(PendingArrivalData pendingArrival)
    {
        Label = AppResources.PendingStamp;
        Address = StampDate.Format(pendingArrival.ArrivalDate);
        Position = new Position(pendingArrival.Latitude, pendingArrival.Longitude);
        Type = PinType.SavedPin;
        Tag = MapPinTag.SetPendingArrivalTag(pendingArrival.Id);
        Anchor = new Point(0.5, 1);
        Icon = PinIcons.For(PinKind.Pending);
    }

    public int Id => ((MapPinTag)Tag).PendingArrivalId;
}
