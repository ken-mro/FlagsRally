namespace FlagsRally.Messages;

/// <summary>
/// Sent through WeakReferenceMessenger after custom boards are reordered or deleted,
/// so pages holding their own board lists can reload them from the database.
/// </summary>
public record CustomBoardsChangedMessage;
