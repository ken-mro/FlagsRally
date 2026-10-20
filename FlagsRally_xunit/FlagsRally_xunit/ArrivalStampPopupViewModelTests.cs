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
}
