using Maui.GoogleMaps;

namespace FlagsRally.Helpers;

public static class MapCameraExtensions
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Moves (or animates) the camera and waits for it, at most <paramref name="timeout"/> (3 s by
    /// default): the task Maui.GoogleMaps returns sometimes never completes.
    /// </summary>
    public static async Task MoveCameraSafelyAsync(this Maui.GoogleMaps.Map map, CameraUpdate update, bool animate, TimeSpan? timeout = null)
    {
        try
        {
            var move = animate ? map.AnimateCamera(update) : map.MoveCamera(update);
            await move.WaitAsync(timeout ?? DefaultTimeout);
        }
        catch (TimeoutException)
        {
#if DEBUG
            Console.WriteLine("Camera move did not complete in time; continuing.");
#endif
        }
    }
}
