namespace FlagsRally.Services;

/// <summary>
/// Holds a custom board location that the map should select when it next appears.
/// A shared singleton rather than a message, because the map page may not exist yet
/// when another page asks for it.
/// </summary>
public class MapFocusRequest
{
    string? _pendingCustomLocationKey;

    public void Request(string customLocationKey) => _pendingCustomLocationKey = customLocationKey;

    public string? Take()
    {
        var key = _pendingCustomLocationKey;
        _pendingCustomLocationKey = null;
        return key;
    }
}
