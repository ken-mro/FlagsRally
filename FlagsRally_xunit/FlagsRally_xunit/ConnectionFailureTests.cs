using System.Net.Sockets;
using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class ConnectionFailureTests
{
    public static TheoryData<Exception, bool> Failures => new()
    {
        // CustomGeolocation wraps what went wrong in "Unable to get location"
        { new Exception("Unable to get location", new HttpRequestException("No such host is known")), true },
        { new Exception("Unable to get location", new TaskCanceledException("timed out", new TimeoutException())), true },
        { new HttpRequestException("reset", new SocketException()), true },
        { new Exception("Unable to get location", new IOException("The connection was aborted")), true },
        // The server answered: an error status or nothing usable. Waiting will not help.
        { new Exception("Unable to get location", new Exception("Unable to get location")), false },
        { new Exception("Unable to get location", new ArgumentException("Two-letter ISO language code")), false },
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public void Only_a_missing_connection_is_worth_waiting_for(Exception exception, bool waits)
    {
        Assert.Equal(waits, ConnectionFailure.IsConnectionProblem(exception));
    }
}
