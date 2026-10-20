namespace FlagsRally.Models.CustomBoard;

public record CustomBoardPinFilterItem
{
    public CustomBoardPinFilterItem(string name)
    {
        Name = name;
        Members = [name];
    }

    /// <summary>
    /// A series of boards shown as one filter, such as the manhole card issues.
    /// </summary>
    public CustomBoardPinFilterItem(string name, IReadOnlyList<string> members)
    {
        Name = name;
        Members = members;
    }

    public CustomBoardPinFilterItem(CustomBoard customBoard)
    {
        Name = customBoard.Name;
    }

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The pin keys (board names) this filter shows; just its own name unless it is a series.
    /// </summary>
    public IReadOnlyList<string> Members { get; init; } = [];
}
