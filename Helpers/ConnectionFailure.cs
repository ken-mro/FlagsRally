using System.Net.Sockets;

namespace FlagsRally.Helpers;

public static class ConnectionFailure
{
    /// <summary>
    /// Whether a lookup failed for want of a connection (no network, a dropped or timed-out request),
    /// so it may work later. A server's answer that it cannot help (an error status, a bad key) is not.
    /// </summary>
    public static bool IsConnectionProblem(Exception exception)
    {
        for (Exception? e = exception; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException or TaskCanceledException or TimeoutException or SocketException or IOException)
                return true;
        }
        return false;
    }
}
