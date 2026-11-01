using SQLite;

namespace FlagsRally.Models;

/// <summary>
/// A stamp put down where there was no connection: the spot and time, checked like any arrival (a real
/// location, not a mock one, within CheckInRange of you), waiting for its country and region to be
/// looked up. Only these two are kept, so what is recorded later cannot differ from where you were.
/// </summary>
[Table("PendingArrival")]
public class PendingArrivalData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime ArrivalDate { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public PendingArrivalData() { } // Required for SQLite

    public PendingArrivalData(DateTime arrivalDate, Location location)
    {
        ArrivalDate = arrivalDate;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
    }

    public Location Location => new(Latitude, Longitude);
}
