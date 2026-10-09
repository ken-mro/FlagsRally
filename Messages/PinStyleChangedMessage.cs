namespace FlagsRally.Messages;

/// <summary>
/// Sent through WeakReferenceMessenger when the map pin style is changed in Settings,
/// so the map can redraw the pins it already shows.
/// </summary>
public record PinStyleChangedMessage;
