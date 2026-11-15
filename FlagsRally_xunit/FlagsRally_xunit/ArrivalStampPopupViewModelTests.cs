using FlagsRally.Models;
using FlagsRally.ViewModels;

namespace FlagsRallyTests.ViewModels;

public class ArrivalStampPopupViewModelTests
{
    [Fact]
    public void A_region_with_its_own_flag_gets_a_second_page()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation
        {
            CountryName = "Japan",
            CountryFlagSource = "🇯🇵",
            AdminAreaName = "Kyoto",
            AdminAreaFlagSource = "jp_26.png",
        });

        Assert.Equal(["Japan", "Kyoto"], vm.Slides.Select(s => s.Name));
        Assert.Equal("🇯🇵", vm.Slides[0].FlagEmoji);
        Assert.Equal("jp_26.png", vm.Slides[1].FlagImage);
        Assert.True(vm.HasSeveral);
        Assert.True(vm.Slides[0].IsCurrent);
    }

    // France has regional flags (Resources/Images/fr), so its regions get their own page
    [Fact]
    public void A_French_region_gets_its_own_page()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation
        {
            CountryName = "France",
            AdminAreaName = "Île-de-France",
            AdminAreaFlagSource = "fr_idf.png",
        });

        Assert.Equal(["France", "Île-de-France"], vm.Slides.Select(s => s.Name));
    }

    // The United Kingdom has no regional flags in the app, so England has no stamp of its own
    [Fact]
    public void A_country_without_regional_flags_shows_only_the_country_stamp()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "United Kingdom", AdminAreaName = "England" });

        Assert.Equal(["United Kingdom"], vm.Slides.Select(s => s.Name));
        Assert.False(vm.HasSeveral);
    }

    [Fact]
    public void An_unexplored_place_says_so_once()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "Unexplored", AdminAreaName = "Unexplored", AdminAreaFlagSource = "x.png" });

        Assert.Single(vm.Slides);
    }

    [Fact]
    public void The_pages_have_no_heading_and_nothing_to_celebrate()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "Japan", AdminAreaName = "Kyoto", AdminAreaFlagSource = "jp_26.png" });

        Assert.All(vm.Slides, s => Assert.False(s.HasHeading));
        Assert.All(vm.Slides, s => Assert.False(s.Celebrates));
    }

    [Theory]
    [InlineData(StampPattern.Square, true, false, false)]
    [InlineData(StampPattern.Ticket, false, true, false)]
    [InlineData(StampPattern.Journey, false, false, true)]
    public void The_stamp_has_the_design_chosen_for_the_passport(StampPattern pattern, bool square, bool shaped, bool journey)
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { Id = 3, CountryName = "Japan" }, StampStyle.Single(pattern));

        var slide = vm.Current;
        Assert.Equal(pattern, slide.Arrival.StampPattern);
        Assert.Equal((square, shaped, journey), (slide.IsSquare, slide.IsShaped, slide.IsJourney));
    }

    [Fact]
    public void A_mixed_passport_gives_the_stamp_its_own_design_but_does_not_tilt_it()
    {
        var arrival = new ArrivalLocation { Id = 7, CountryName = "Japan" };

        var vm = new ArrivalStampPopupViewModel(arrival, StampStyle.Mixed);

        Assert.Equal(StampPatterns.For(arrival, StampStyle.Mixed), vm.Current.Arrival.StampPattern);
        Assert.Equal(0, vm.Current.Arrival.StampTilt);
    }

    [Fact]
    public void Swiping_moves_between_the_country_and_the_region()
    {
        var vm = new ArrivalStampPopupViewModel(new ArrivalLocation { CountryName = "Japan", AdminAreaName = "Kyoto", AdminAreaFlagSource = "jp_26.png" });

        Assert.True(vm.Move(1));
        Assert.Equal("Kyoto", vm.Current.Name);
        Assert.False(vm.Move(1));
        Assert.True(vm.Move(-1));
        Assert.Equal("Japan", vm.Current.Name);
    }
}
