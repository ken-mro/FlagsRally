namespace FlagsRally.Messages;

/// <summary>
/// Sent through WeakReferenceMessenger after custom boards are added, deleted or reordered,
/// so pages holding their own board lists can reload them from the database.
/// </summary>
/// <param name="PlacesChanged">False when only the order changed, so the map keeps its pins.</param>
public record CustomBoardsChangedMessage(bool PlacesChanged = true);
