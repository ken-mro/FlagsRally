namespace FlagsRally.Messages;

/// <summary>
/// Sent through WeakReferenceMessenger when another app shares a place with this one (see
/// <see cref="Services.MapFocusRequest"/>), so a map already on screen shows it.
/// </summary>
public record SharedPlaceReceivedMessage;
