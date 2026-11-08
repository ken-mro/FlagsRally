namespace FlagsRally.Services;

/// <summary>
/// Holds a custom board location or an arrival that the map should select when it next appears, or what another app
/// shared for the map to show. A shared singleton rather than a message, because the map page may not
/// exist yet when another page (or app) asks for it.
/// </summary>
public class MapFocusRequest
{
    string? _pendingCustomLocationKey;
    int? _pendingArrivalLocationId;
    string? _pendingSharedText;

    // Only the latest request counts: asking for a place replaces an earlier arrival, and the other way round.
    public void Request(string customLocationKey)
    {
        _pendingCustomLocationKey = customLocationKey;
        _pendingArrivalLocationId = null;
    }

    /// <summary>An arrival (a passport stamp) to select on the map.</summary>
    public void RequestArrival(int arrivalLocationId)
    {
        _pendingArrivalLocationId = arrivalLocationId;
        _pendingCustomLocationKey = null;
    }

    public int? TakeArrival()
    {
        var id = _pendingArrivalLocationId;
        _pendingArrivalLocationId = null;
        return id;
    }

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
