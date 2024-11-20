using FlagsRally.Resources;

namespace FlagsRally.Models.CustomBoard;

public record CustomBoardPinFilterItem
{
    const int ALL_PINS_ID = -1;
    const int ARRIVAL_PINS_ID = 0;

    public CustomBoardPinFilterItem(string name)
    {
        Name = name;
    }

    public CustomBoardPinFilterItem(CustomBoard customBoard)
    {
        Name = customBoard.Name;
    }

    public string Name { get; init; } = string.Empty;
    public bool IsAll => (All()).Equals(this);

    public static IEnumerable<CustomBoardPinFilterItem> CreateFilterList()
    {
        return [All()];
    }

    private static CustomBoardPinFilterItem All()
    {
        return new CustomBoardPinFilterItem(AppResources.AllPins);
    }
}
