using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

// The location page must always lead on to the permission request (App Store guideline 5.1.1(iv))
public class OnboardingViewModelTests
{
    [Fact]
    public void Skip_goes_to_the_location_page_instead_of_closing()
    {
        var closed = false;
        var vm = new OnboardingViewModel(() => { closed = true; return Task.CompletedTask; });

        vm.SkipCommand.Execute(null);

        Assert.False(closed);
        Assert.True(vm.IsLast);
        Assert.Equal(OnboardingStep.Location, vm.Step);
    }

    [Fact]
    public void The_location_page_offers_only_the_way_on_to_the_permission_request()
    {
        var vm = new OnboardingViewModel(() => Task.CompletedTask);
        vm.SkipCommand.Execute(null);

        Assert.True(vm.ShowsAllowLocation);
        Assert.False(vm.ShowsDone);
    }
}
