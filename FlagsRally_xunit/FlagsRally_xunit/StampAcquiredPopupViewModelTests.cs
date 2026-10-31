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
        var slides = StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Single(StampPattern.Ticket), newCountry: true, hasRegions: true, newRegion: true);

        Assert.Equal(["Japan", "Kyoto"], slides.Select(s => s.Name));
        Assert.All(slides, s => Assert.True(s.Celebrates));
        Assert.True(slides[0].HasFlagEmoji);
        Assert.True(slides[1].HasFlagImage);
        Assert.All(slides, s => Assert.Equal(StampPattern.Ticket, s.Arrival.StampPattern));
        Assert.All(slides, s => Assert.Equal(0, s.Arrival.StampTilt));
    }

    // A country with regional flags always shows both stamps; only what is new gets confetti
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void A_country_with_regional_flags_always_stamps_the_region_too(bool newCountry, bool newRegion)
    {
        var slides = StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, newCountry, hasRegions: true, newRegion);

        Assert.Equal(["Japan", "Kyoto"], slides.Select(s => s.Name));
        Assert.Equal([newCountry, newRegion], slides.Select(s => s.Celebrates));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_country_without_regional_flags_is_one_page(bool newCountry)
    {
        var slide = Assert.Single(StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, newCountry, hasRegions: false, newRegion: false));

        Assert.Equal("Japan", slide.Name);
        Assert.True(slide.HasFlagEmoji);
        Assert.Equal(newCountry, slide.Celebrates);
    }

    [Fact]
    public void Pages_move_within_their_ends_and_mark_the_current_one()
    {
        var vm = new StampAcquiredPopupViewModel(StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, true, true, true));

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
        var vm = new StampAcquiredPopupViewModel(StampAcquiredPopupViewModel.SlidesFor(Kyoto, StampStyle.Default, true, false, false));

        Assert.False(vm.CanClose);
    }
}
