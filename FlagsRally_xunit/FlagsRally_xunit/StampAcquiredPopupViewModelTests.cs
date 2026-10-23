using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class StampAcquiredPopupViewModelTests
{
    static readonly ArrivalLocation Kyoto = new()
    {
        Id = 4,
        CountryName = "Japan",
        CountryFlagSource = "🇯🇵",
        AdminAreaName = "Kyoto",
        AdminAreaFlagSource = "jp_26.png",
    };

    [Fact]
    public void A_new_country_and_region_are_two_pages_country_first()
    {
        var slides = StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Single(StampPattern.Ticket), newCountry: true, newRegion: true);

        Assert.Equal(["Japan", "Kyoto"], slides.Select(s => s.Name));
        Assert.True(slides[0].HasFlagEmoji);
        Assert.True(slides[1].HasFlagImage);
        Assert.All(slides, s => Assert.Equal(StampPattern.Ticket, s.Arrival.StampPattern));
        Assert.All(slides, s => Assert.Equal(0, s.Arrival.StampTilt));
    }

    [Theory]
    [InlineData(true, false, "Japan")]
    [InlineData(false, true, "Kyoto")]
    public void Only_what_is_new_gets_a_page(bool newCountry, bool newRegion, string name)
    {
        var slides = StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, newCountry, newRegion);

        Assert.Equal(name, Assert.Single(slides).Name);
    }

    [Fact]
    public void Pages_move_within_their_ends_and_mark_the_current_one()
    {
        var vm = new StampAcquiredPopupViewModel(StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, true, true));

        Assert.False(vm.Move(-1));
        Assert.True(vm.HasMore);
        Assert.True(vm.Move(1));
        Assert.True(vm.IsLast);
        Assert.False(vm.Move(1));
        Assert.Equal([false, true], vm.Slides.Select(s => s.IsCurrent));
    }

    [Fact]
    public void Ok_waits_until_the_view_says_every_page_has_been_pressed()
    {
        var vm = new StampAcquiredPopupViewModel(StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, true, false));

        Assert.False(vm.CanClose);
    }
}
