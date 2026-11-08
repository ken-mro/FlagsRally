using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class ArrivalInfoPopupViewModelTests
{
    static ArrivalInfoPopupViewModel Create() => new(new ArrivalLocation
    {
        CountryName = "Japan",
        AdminAreaName = "Tokyo",
        LocalityName = "Minato",
        Location = new Location(35.65858512, 139.74543212),
    });

    [Fact]
    public void Shows_the_stamps_details_with_rounded_coordinates()
    {
        var vm = Create();

        Assert.Equal("Japan", vm.CountryName);
        Assert.Equal("Tokyo", vm.AdminAreaName);
        Assert.Equal("Minato", vm.LocalityName);
        Assert.Equal($"{35.658585}, {139.745432}", vm.Coordinates);
    }

    [Fact]
    public void Closing_without_a_choice_is_close()
    {
        Assert.Equal(ArrivalInfoChoice.Close, Create().Choice);
    }

    [Fact]
    public async Task Each_button_records_its_choice()
    {
        var vm = Create();
        await vm.ShowOnMapCommand.ExecuteAsync(null);
        Assert.Equal(ArrivalInfoChoice.ShowOnMap, vm.Choice);

        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(ArrivalInfoChoice.Delete, vm.Choice);

        await vm.CloseCommand.ExecuteAsync(null);
        Assert.Equal(ArrivalInfoChoice.Close, vm.Choice);
    }
}
