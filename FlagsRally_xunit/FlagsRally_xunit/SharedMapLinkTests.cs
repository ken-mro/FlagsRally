using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class SharedMapLinkTests
{
    // What Google Maps' Share gives on Android: the short link alone, or after the name and address
    [Theory]
    [InlineData("https://maps.app.goo.gl/khuCY21zJbc62BF89?g_st=ac", "https://maps.app.goo.gl/khuCY21zJbc62BF89?g_st=ac")]
    [InlineData("Kinkaku-ji\n1 Kinkakujicho, Kita Ward, Kyoto\nhttps://maps.app.goo.gl/abc", "https://maps.app.goo.gl/abc")]
    public void Finds_the_link_in_what_was_shared(string text, string link)
    {
        Assert.Equal(link, SharedMapLink.FindLink(text)?.OriginalString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Let's meet at the temple")]
    public void Nothing_without_a_link(string? text)
    {
        Assert.Null(SharedMapLink.FindLink(text));
    }

    [Theory]
    [InlineData("https://maps.app.goo.gl/abc", true)]
    [InlineData("https://goo.gl/maps/abc", true)]
    [InlineData("https://www.google.com/maps/place/Kinkaku-ji", true)]
    [InlineData("https://www.google.co.jp/maps/@35.03,135.72,17z", true)]
    [InlineData("https://maps.google.com/?q=35.03,135.72", true)]
    [InlineData("http://maps.app.goo.gl/abc", false)]
    [InlineData("https://goo.gl/abc", false)]
    [InlineData("https://google.com.evil.example/maps", false)]
    [InlineData("https://evil.example/maps.app.goo.gl", false)]
    [InlineData("https://maps.apple.com/?ll=35.03,135.72", false)]
    public void Follows_only_Google_Maps_over_https(string link, bool follows)
    {
        Assert.Equal(follows, SharedMapLink.IsGoogleMaps(new Uri(link)));
    }

    [Theory]
    // The place's own position wins over where the map was looking
    [InlineData("https://www.google.com/maps/place/Kinkaku-ji/@35.0393,135.7268,17z/data=!3m1!4b1!4m6!3m5!1s0x0:0x0!8m2!3d35.0394!4d135.7292", 35.0394, 135.7292)]
    [InlineData("https://www.google.com/maps/@35.0393,135.7268,17z", 35.0393, 135.7268)]
    [InlineData("https://maps.google.com/?q=35.0394,135.7292", 35.0394, 135.7292)]
    [InlineData("https://maps.google.com/maps?q=35.0394%2C135.7292&z=17", 35.0394, 135.7292)]
    [InlineData("https://www.google.com/maps/search/35.0394,+135.7292?entry=tts", 35.0394, 135.7292)]
    [InlineData("https://www.google.com/maps/search/?api=1&query=-33.8568,151.2153", -33.8568, 151.2153)]
    [InlineData("https://www.google.com/maps/place/40%C2%B041'21.4%22N/@40.6892,-74.0445,17z/data=!3m1!4b1!4m4!3m3!8m2!3d40.6892!4d-74.0445", 40.6892, -74.0445)]
    [InlineData("geo:35.0394,135.7292?q=Kinkaku-ji", 35.0394, 135.7292)]
    public void Reads_the_coordinates_in_a_link(string link, double latitude, double longitude)
    {
        var location = SharedMapLink.FindCoordinates(link);

        Assert.NotNull(location);
        Assert.Equal(latitude, location.Latitude, 6);
        Assert.Equal(longitude, location.Longitude, 6);
    }

    [Theory]
    // Where the short link leads: a name and address, no coordinates
    [InlineData("https://www.google.com/maps/place/Kinkaku-ji,+1+Kinkakujicho,+Kita+Ward,+Kyoto,+603-8361,+Japan/data=!4m2!3m1!1s0x6001a820c0eb46bd:0xee4272b1c22645f?utm_source=mstt_1&entry=gps")]
    [InlineData("https://maps.google.com/maps?q=Kinkaku-ji&ftid=0x6001a820c0eb46bd:0xee4272b1c22645f")]
    [InlineData("geo:0,0?q=Kinkaku-ji")]
    [InlineData("https://maps.google.com/?q=91,135")]
    [InlineData("https://maps.google.com/?q=35,181")]
    public void No_coordinates_when_the_link_has_none(string link)
    {
        Assert.Null(SharedMapLink.FindCoordinates(link));
    }

    [Theory]
    [InlineData("https://www.google.com/maps/place/Kinkaku-ji,+1+Kinkakujicho,+Kita+Ward,+Kyoto,+603-8361,+Japan/data=!4m2!3m1!1s0x6001a820c0eb46bd:0xee4272b1c22645f?entry=gps",
        "Kinkaku-ji, 1 Kinkakujicho, Kita Ward, Kyoto, 603-8361, Japan", "Kinkaku-ji")]
    [InlineData("https://www.google.com/maps/place/%E9%87%91%E9%96%A3%E5%AF%BA/data=!4m2", "金閣寺", "金閣寺")]
    [InlineData("https://maps.google.com/maps?q=Kinkaku-ji&ftid=0x6001a820c0eb46bd:0xee4272b1c22645f", "Kinkaku-ji", "Kinkaku-ji")]
    public void Reads_the_name_and_address_to_look_up(string link, string placeText, string name)
    {
        var text = SharedMapLink.FindPlaceText(new Uri(link));

        Assert.Equal(placeText, text);
        Assert.Equal(name, SharedMapLink.ShortName(text));
    }

    [Theory]
    [InlineData("Tokyo Tower https://www.google.com/maps/search/35.6586,+139.7454", "Tokyo Tower")]
    [InlineData("Kinkaku-ji\n1 Kinkakujicho, Kita Ward, Kyoto\nhttps://maps.app.goo.gl/abc", "Kinkaku-ji")]
    [InlineData("https://maps.app.goo.gl/abc", null)]
    [InlineData("A note far longer than any place name would be, written before the link that follows it https://maps.app.goo.gl/abc", null)]
    public void Takes_the_name_written_before_the_link(string text, string? name)
    {
        Assert.Equal(name, SharedMapLink.NameBeforeLink(text));
    }

    [Fact]
    public void A_place_given_as_coordinates_has_no_name()
    {
        Assert.Null(SharedMapLink.ShortName("35.0394, 135.7292"));
    }

    [Fact]
    public void No_place_text_in_a_link_to_nowhere_in_particular()
    {
        Assert.Null(SharedMapLink.FindPlaceText(new Uri("https://www.google.com/maps/@35.03,135.72,17z")));
    }
}
