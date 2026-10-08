namespace FlagsRally.Messages;

/// <summary>
/// Sent through WeakReferenceMessenger after custom boards are reordered or deleted,
/// so pages holding their own board lists can refresh.
/// </summary>
public record CustomBoardsChangedMessage(IReadOnlyCollection<string> DeletedBoardNames)
{
    public static CustomBoardsChangedMessage Reordered() => new([]);
}
