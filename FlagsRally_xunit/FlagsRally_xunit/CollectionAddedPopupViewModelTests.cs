using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class CollectionAddedPopupViewModelTests
{
    [Fact]
    public void The_bar_moves_on_by_the_new_region()
    {
        var vm = new CollectionAddedPopupViewModel(new SubRegion { Name = "Mecklenburg-Vorpommern" }, "Germany", visited: 3, total: 16);

        Assert.Equal(2 / 16.0, vm.ProgressBefore);
        Assert.Equal(3 / 16.0, vm.ProgressAfter);
        Assert.Equal("3 / 16", vm.ProgressText);
        Assert.False(vm.CanClose);
    }

    [Fact]
    public void The_first_region_starts_the_bar_from_empty()
    {
        var vm = new CollectionAddedPopupViewModel(new SubRegion { Name = "Kyoto" }, "Japan", visited: 1, total: 47);

        Assert.Equal(0, vm.ProgressBefore);
    }
}
