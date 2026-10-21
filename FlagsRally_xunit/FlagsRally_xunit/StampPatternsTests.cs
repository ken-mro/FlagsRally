using FlagsRally.Models;

namespace FlagsRallyTests.Models;

public class StampPatternsTests
{
    static ArrivalLocation Arrival(int id, string country) => new() { Id = id, CountryCode = country };

    [Fact]
    public void The_original_square_is_the_default()
    {
        Assert.Equal(StampStyle.Single(StampPattern.Square), StampStyle.FromKey(null));
        Assert.Equal(StampStyle.Default, StampStyle.FromKey("Nonsense"));
    }

    [Theory]
    [InlineData("Mixed")]
    [InlineData("ByCountry")]
    [InlineData("Square")]
    [InlineData("Sunrise")]
    [InlineData("Airmail")]
    [InlineData("Triangle")]
    [InlineData("Ticket")]
    public void A_style_survives_being_saved(string key)
    {
        Assert.Equal(key, StampStyle.FromKey(key).Key);
    }

    [Fact]
    public void One_pattern_for_all_draws_every_stamp_upright_in_it()
    {
        var style = StampStyle.Single(StampPattern.WorldOval);

        foreach (var id in Enumerable.Range(1, 30))
        {
            var stamp = StampPatterns.Apply(Arrival(id, "JP"), style);
            Assert.Equal(StampPattern.WorldOval, stamp.StampPattern);
            Assert.Equal(0, stamp.StampTilt);
        }
    }

    [Fact]
    public void A_mix_uses_every_pattern_and_keeps_each_stamp_the_same()
    {
        var patterns = Enumerable.Range(1, 200).Select(id => StampPatterns.For(Arrival(id, "JP"), StampStyle.Mixed)).ToList();

        Assert.Equal(StampPatterns.All.Count, patterns.Distinct().Count());
        Assert.Equal(patterns, Enumerable.Range(1, 200).Select(id => StampPatterns.For(Arrival(id, "JP"), StampStyle.Mixed)));
    }

    [Fact]
    public void Per_country_stamps_of_one_country_share_a_pattern()
    {
        var japan = Enumerable.Range(1, 20).Select(id => StampPatterns.For(Arrival(id, "JP"), StampStyle.PerCountry)).Distinct();

        Assert.Single(japan);
        Assert.Equal(StampPatterns.For(Arrival(1, "jp"), StampStyle.PerCountry), StampPatterns.For(Arrival(2, "JP"), StampStyle.PerCountry));
    }

    [Fact]
    public void Mixed_stamps_lean_a_little_either_way()
    {
        var tilts = Enumerable.Range(1, 200).Select(id => StampPatterns.TiltFor(Arrival(id, "JP"), StampStyle.Mixed)).ToList();

        Assert.All(tilts, t => Assert.InRange(t, -4, 4));
        Assert.Contains(tilts, t => t < 0);
        Assert.Contains(tilts, t => t > 0);
    }

    [Fact]
    public void The_square_keeps_its_black_ink_and_red_date()
    {
        Assert.Equal(Color.FromArgb("#22252E"), StampPatterns.InkFor(StampPattern.Square));
        Assert.Equal(Color.FromArgb("#B94047"), StampPatterns.DateInkFor(StampPattern.Square));
        Assert.Equal(StampPatterns.InkFor(StampPattern.ScallopedGlobe), StampPatterns.DateInkFor(StampPattern.ScallopedGlobe));
    }

    [Fact]
    public void An_old_saved_design_falls_back_to_the_square()
    {
        Assert.Equal(StampStyle.Default, StampStyle.FromKey("Octagon"));
        Assert.Equal(StampStyle.Default, StampStyle.FromKey("GlobeSeal"));
    }

    [Fact]
    public void Every_design_but_the_square_has_its_own_ink_and_room_for_its_motifs()
    {
        var shaped = StampPatterns.All.Where(p => p != StampPattern.Square).ToList();

        Assert.Equal(shaped.Count, shaped.Select(StampPatterns.InkFor).Distinct().Count());
        Assert.All(shaped, p => Assert.True(StampPatterns.PaddingFor(p).Top >= 12 && StampPatterns.PaddingFor(p).Left >= 20));
    }

    [Fact]
    public void Only_designs_with_room_for_it_say_arrival()
    {
        Assert.True(StampPatterns.ShowsArrival(StampPattern.Square));
        Assert.False(StampPatterns.ShowsArrival(StampPattern.ScallopedGlobe)); // globe above, travel row below
        Assert.True(StampPatterns.ShowsArrival(StampPattern.Postcard));  // motifs in the corners only
    }
}
