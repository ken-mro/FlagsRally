using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class GridSpanTests
{
    // Window sizes in points. Opened: Galaxy Z Fold 5 (about 777 x 933), iPhone Duo (reported 669 x 951).
    [Theory]
    [InlineData(344, 882, false)]  // Z Fold 5 cover screen
    [InlineData(466, 678, false)]  // iPhone Duo outer screen
    [InlineData(411, 914, false)]  // a phone upright
    [InlineData(914, 411, false)]  // the same phone on its side
    [InlineData(777, 933, true)]
    [InlineData(933, 777, true)]
    [InlineData(669, 951, true)]
    [InlineData(951, 669, true)]
    public void A_window_is_opened_when_its_shorter_side_is_wide(double width, double height, bool opened)
    {
        Assert.Equal(opened, GridSpan.IsOpened(width, height));
    }

    // Opening shows twice the columns the closed phone had (two)
    [Theory]
    [InlineData(777)]
    [InlineData(669)]
    [InlineData(933)]
    [InlineData(951)]
    public void An_opened_foldable_shows_twice_the_columns_of_its_closed_screen(double pageWidth)
    {
        Assert.Equal(2, GridSpan.For(344, opened: false));
        Assert.Equal(2, GridSpan.For(466, opened: false));
        Assert.Equal(4, GridSpan.For(pageWidth, opened: true));
    }

    // Turning a phone on its side is not opening it: the count by width stays as it was
    [Theory]
    [InlineData(411, 2)]
    [InlineData(600, 3)]
    [InlineData(914, 4)]
    public void A_phone_that_is_not_opened_keeps_the_count_by_width(double pageWidth, int span)
    {
        Assert.Equal(span, GridSpan.For(pageWidth));
        Assert.Equal(span, GridSpan.For(pageWidth, opened: false));
    }

    // A tablet wide enough for more than four columns keeps them
    [Fact]
    public void An_opened_screen_never_shows_fewer_columns_than_its_width_fits()
    {
        Assert.Equal(6, GridSpan.For(1180, opened: true));
    }

    [Fact]
    public void Collection_cards_are_two_in_a_row_and_four_when_opened()
    {
        Assert.Equal(2, GridSpan.ForCards(opened: false));
        Assert.Equal(4, GridSpan.ForCards(opened: true));
    }
}
