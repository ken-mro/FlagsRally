using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class ArrivalStampPopupViewModelTests
{
    [Fact]
    public void A_region_is_shown_with_its_flag_under_the_country()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation
        {
            CountryName = "Japan",
            AdminAreaName = "Kyoto",
            AdminAreaFlagSource = "jp_26.png",
        });

        Assert.True(vm.HasAdminAreaName);
        Assert.True(vm.HasAdminAreaFlag);
    }

    [Fact]
    public void A_country_without_regional_flags_shows_only_the_country_flag()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "France", AdminAreaName = "Île-de-France" });

        Assert.True(vm.HasAdminAreaName);
        Assert.False(vm.HasAdminAreaFlag);
    }

    [Fact]
    public void An_unexplored_place_says_so_once()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "Unexplored", AdminAreaName = "Unexplored" });

        Assert.False(vm.HasAdminAreaName);
    }

    [Theory]
    [InlineData(StampPattern.Square, true, false, false)]
    [InlineData(StampPattern.Ticket, false, true, false)]
    [InlineData(StampPattern.Journey, false, false, true)]
    public void The_stamp_has_the_design_chosen_for_the_passport(StampPattern pattern, bool square, bool shaped, bool journey)
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { Id = 3, CountryName = "Japan" }, StampStyle.Single(pattern));

        Assert.Equal(pattern, vm.Arrival.StampPattern);
        Assert.Equal((square, shaped, journey), (vm.IsSquare, vm.IsShaped, vm.IsJourney));
    }

    [Fact]
    public void A_mixed_passport_gives_the_stamp_its_own_design_but_does_not_tilt_it()
    {
        var arrival = new ArrivalLocation { Id = 7, CountryName = "Japan" };

        var vm = new ArrivalStampPopupViewModel(arrival, StampStyle.Mixed);

        Assert.Equal(StampPatterns.For(arrival, StampStyle.Mixed), vm.Arrival.StampPattern);
        Assert.Equal(0, vm.Arrival.StampTilt);
    }
}
