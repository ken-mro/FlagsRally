using FlagsRally.Models.CustomBoard;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class PinFilterPopupViewModelTests
{
    private static List<CustomBoardPinFilterItem> CreateItems() =>
        [.. CustomBoardPinFilterItem.CreateFilterList(), new("Board A"), new("Board B")];

    [Fact]
    public void Empty_selection_checks_all_pins_only()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), []);

        Assert.True(vm.Options.Single(o => o.IsAll).IsSelected);
        Assert.All(vm.Options.Where(o => !o.IsAll), o => Assert.False(o.IsSelected));
    }

    [Fact]
    public void Selecting_a_board_unchecks_all_pins()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), []);

        vm.Options.Single(o => o.Name == "Board A").IsSelected = true;

        Assert.False(vm.Options.Single(o => o.IsAll).IsSelected);
    }

    [Fact]
    public void Selecting_all_pins_unchecks_boards()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), ["Board A", "Board B"]);

        vm.Options.Single(o => o.IsAll).IsSelected = true;

        Assert.All(vm.Options.Where(o => !o.IsAll), o => Assert.False(o.IsSelected));
    }

    [Fact]
    public async Task Confirm_returns_selected_boards()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), ["Board A"]);
        vm.Options.Single(o => o.Name == "Board B").IsSelected = true;

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal(["Board A", "Board B"], vm.Result);
    }

    [Fact]
    public async Task Confirm_with_nothing_selected_returns_show_all()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), ["Board A"]);
        vm.Options.Single(o => o.Name == "Board A").IsSelected = false;

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.NotNull(vm.Result);
        Assert.Empty(vm.Result);
    }

    [Fact]
    public async Task Cancel_returns_null()
    {
        var vm = new PinFilterPopupViewModel(CreateItems(), []);

        await vm.CancelCommand.ExecuteAsync(null);

        Assert.Null(vm.Result);
    }
}
