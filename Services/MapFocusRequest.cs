namespace FlagsRally.Services;

/// <summary>
/// Holds a custom board location that the map should select when it next appears, or what another app
/// shared for the map to show. A shared singleton rather than a message, because the map page may not
/// exist yet when another page (or app) asks for it.
/// </summary>
public class MapFocusRequest
{
    string? _pendingCustomLocationKey;
    string? _pendingSharedText;

    public void Request(string customLocationKey) => _pendingCustomLocationKey = customLocationKey;

    public string? Take()
    {
        var key = _pendingCustomLocationKey;
        _pendingCustomLocationKey = null;
        return key;
    }

    /// <summary>What another app shared (a map link), to be found and marked on the map.</summary>
    public void RequestSharedPlace(string sharedText) => _pendingSharedText = sharedText;

    public string? TakeSharedPlace()
    {
        var text = _pendingSharedText;
        _pendingSharedText = null;
        return text;
    }
}
