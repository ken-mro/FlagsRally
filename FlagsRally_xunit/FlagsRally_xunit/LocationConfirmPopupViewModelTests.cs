using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class LocationConfirmPopupViewModelTests
{
    static ArrivalLocationData Fukushima() => new(
        new DateTime(2026, 11, 15), "Japan", "Fukushima", "Ishikawa", "Japan", "Fukushima", "Ishikawa", "JP", "07",
        new Location(37.1448981234, 140.4510512345));

    [Fact]
    public void The_place_is_listed_like_a_passport_stamp_without_a_date()
    {
        var vm = new LocationConfirmPopupViewModel("Is the following your current location?", Fukushima());

        Assert.Equal("Is the following your current location?", vm.Message);
        Assert.Equal(string.Empty, vm.Date);
        Assert.Equal("Japan", vm.CountryName);
        Assert.Equal("Fukushima", vm.AdminAreaName);
        Assert.Equal("Ishikawa", vm.LocalityName);
        Assert.Equal("37.144898, 140.451051", vm.Coordinates);
    }

    [Fact]
    public async Task Only_Yes_confirms()
    {
        var vm = new LocationConfirmPopupViewModel("?", Fukushima());
        Assert.False(vm.Confirmed);

        await vm.NoCommand.ExecuteAsync(null);
        Assert.False(vm.Confirmed);

        await vm.YesCommand.ExecuteAsync(null);
        Assert.True(vm.Confirmed);
    }
}
