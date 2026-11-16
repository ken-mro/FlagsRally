using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class VisitProgressTests
{
    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(2, 47, 2 / 47.0)]
    [InlineData(3, 3, 1.0)]
    public void Ratio_is_zero_for_an_empty_board(int visited, int total, double expected) =>
        Assert.Equal(expected, VisitProgress.Ratio(visited, total), 6);

    [Fact]
    public void Text_shows_visited_of_total() => Assert.Equal("2 / 47", VisitProgress.Text(2, 47));
}
