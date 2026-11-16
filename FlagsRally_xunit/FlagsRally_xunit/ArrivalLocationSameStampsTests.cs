using FlagsRally.Models;

namespace FlagsRallyTests.Models;

public class ArrivalLocationSameStampsTests
{
    // Read again from the database: new objects, a new Location each
    static List<ArrivalLocation> Stamps() =>
    [
        new() { Id = 1, ArrivalDate = new DateTime(2026, 10, 10), CountryCode = "JP", CountryName = "Japan", AdminAreaName = "Mie", Location = new Location(34.45, 136.72) },
        new() { Id = 2, ArrivalDate = new DateTime(2026, 10, 18), CountryCode = "JP", CountryName = "Japan", AdminAreaName = "Kyoto", Location = new Location(35.01, 135.78) },
    ];

    [Fact]
    public void Stamps_read_again_show_the_same()
    {
        Assert.True(ArrivalLocation.ShowTheSame(Stamps(), Stamps()));
        Assert.True(ArrivalLocation.ShowTheSame([], []));
    }

    [Fact]
    public void A_new_or_a_deleted_stamp_shows()
    {
        var more = Stamps();
        more.Add(new() { Id = 3, CountryCode = "FR", CountryName = "France" });

        Assert.False(ArrivalLocation.ShowTheSame(Stamps(), more));
        Assert.False(ArrivalLocation.ShowTheSame(more, Stamps()));
        Assert.False(ArrivalLocation.ShowTheSame([], Stamps()));
    }

    [Fact]
    public void Another_order_shows()
    {
        var reversed = Stamps();
        reversed.Reverse();

        Assert.False(ArrivalLocation.ShowTheSame(Stamps(), reversed));
    }

    // Settings > Passport stamps: another design or tilt redraws the stamps
    [Fact]
    public void Another_design_or_tilt_shows()
    {
        var stamps = Stamps();

        Assert.False(ArrivalLocation.ShowTheSame(stamps, [stamps[0] with { StampPattern = StampPattern.Airmail }, stamps[1]]));
        Assert.False(ArrivalLocation.ShowTheSame(stamps, [stamps[0], stamps[1] with { StampTilt = 3 }]));
    }

    [Fact]
    public void A_changed_name_or_place_shows()
    {
        var stamps = Stamps();

        Assert.False(ArrivalLocation.ShowTheSame(stamps, [stamps[0] with { AdminAreaName = "三重県" }, stamps[1]]));
        Assert.False(ArrivalLocation.ShowTheSame(stamps, [stamps[0] with { Location = new Location(34.46, 136.72) }, stamps[1]]));
    }
}
