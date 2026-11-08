using FlagsRally.Services;

namespace FlagsRallyTests.Services;

public class MapFocusRequestTests
{
    [Fact]
    public void An_arrival_is_taken_once()
    {
        var request = new MapFocusRequest();
        request.RequestArrival(12);

        Assert.Equal(12, request.TakeArrival());
        Assert.Null(request.TakeArrival());
        Assert.Null(request.Take());
    }

    [Fact]
    public void The_latest_request_wins()
    {
        var request = new MapFocusRequest();

        request.Request("Board-1");
        request.RequestArrival(3);
        Assert.Null(request.Take());
        Assert.Equal(3, request.TakeArrival());

        request.RequestArrival(4);
        request.Request("Board-2");
        Assert.Null(request.TakeArrival());
        Assert.Equal("Board-2", request.Take());
    }
}
