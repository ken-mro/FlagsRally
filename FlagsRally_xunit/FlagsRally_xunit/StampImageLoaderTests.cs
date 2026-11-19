using System.Net;
using FlagsRally.Services;

namespace FlagsRallyTests.Services;

public class StampImageLoaderTests
{
    static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];

    sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return answer(request, cancellationToken);
        }
    }

    static FakeHandler Answer(HttpStatusCode status, byte[] body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(body) }));

    [Fact]
    public async Task A_picture_is_returned()
    {
        var loader = new StampImageLoader(Answer(HttpStatusCode.OK, Png));

        Assert.Equal(Png, await loader.LoadAsync("https://example.com/stamp.png"));
    }

    // The case that showed confetti over an empty frame: the image never arrives
    [Fact]
    public async Task No_connection_gives_null()
    {
        var loader = new StampImageLoader(new FakeHandler((_, _) => throw new HttpRequestException("offline")));

        Assert.Null(await loader.LoadAsync("https://example.com/stamp.png"));
    }

    [Fact]
    public async Task An_error_status_gives_null()
    {
        var loader = new StampImageLoader(Answer(HttpStatusCode.NotFound, Png));

        Assert.Null(await loader.LoadAsync("https://example.com/stamp.png"));
    }

    [Fact]
    public async Task An_html_page_is_not_a_stamp()
    {
        var loader = new StampImageLoader(Answer(HttpStatusCode.OK, "<html>Sign in</html>"u8.ToArray()));

        Assert.Null(await loader.LoadAsync("https://example.com/stamp.png"));
    }

    [Fact]
    public async Task An_image_larger_than_the_limit_gives_null()
    {
        var huge = new byte[StampImageLoader.MaxBytes + 1];
        Png.CopyTo(huge, 0);
        var loader = new StampImageLoader(Answer(HttpStatusCode.OK, huge));

        Assert.Null(await loader.LoadAsync("https://example.com/stamp.png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("file:///etc/passwd")]
    public async Task Only_web_addresses_are_fetched(string url)
    {
        var handler = Answer(HttpStatusCode.OK, Png);

        Assert.Null(await new StampImageLoader(handler).LoadAsync(url));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void Picture_formats_are_told_by_their_bytes()
    {
        Assert.True(StampImageLoader.IsPicture(Png));
        Assert.True(StampImageLoader.IsPicture([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.True(StampImageLoader.IsPicture("GIF89a"u8));
        Assert.True(StampImageLoader.IsPicture("RIFF\0\0\0\0WEBPVP8 "u8));
        Assert.False(StampImageLoader.IsPicture("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8));
        Assert.False(StampImageLoader.IsPicture([]));
    }
}
