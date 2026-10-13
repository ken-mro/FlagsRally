using FlagsRally.Resources;

namespace FlagsRally.Models.CustomBoard;

public record CustomBoardPinFilterItem
{
    const int ALL_PINS_ID = -1;
    const int ARRIVAL_PINS_ID = 0;

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
    // By name: the record's own equality would also compare the Members lists, which are never the same instance.
    public bool IsAll => Name == AppResources.AllPins && Members.Count == 1;

    public static IEnumerable<CustomBoardPinFilterItem> CreateFilterList()
    {
        return [All()];
    }

    private static CustomBoardPinFilterItem All()
    {
        return new CustomBoardPinFilterItem(AppResources.AllPins);
    }
}
