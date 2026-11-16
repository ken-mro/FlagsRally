using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class SideTabBarTests
{
    [Theory]
    [InlineData(0, 84, true)]    // iPhone Duo: the tab bar's strip at the right
    [InlineData(0, 0, false)]    // a phone held upright
    [InlineData(62, 62, false)]  // a phone on its side: the camera at one side, the same width at the other
    [InlineData(62, 0, false)]
    public void A_tab_bar_at_the_side_takes_only_the_right_of_the_safe_area(double left, double right, bool atSide)
    {
        Assert.Equal(atSide, SideTabBar.IsAtSide(left, right));
    }

    [Fact]
    public void A_list_is_taken_out_to_the_right_and_lower_edge()
    {
        var margin = SideTabBar.MarginToEdges(new Thickness(8, 0), rightInset: 84, bottomInset: 34);

        Assert.Equal(new Thickness(8, 0, -76, -34), margin);
    }
}
