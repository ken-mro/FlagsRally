using FlagsRally.Resources;

namespace FlagsRally.Models.CustomBoard;

public record CustomBoardPinFilterItem
{
    const int ALL_PINS_ID = -1;
    const int ARRIVAL_PINS_ID = 0;

    public CustomBoardPinFilterItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public CustomBoardPinFilterItem(CustomBoard customBoard)
    {
        Id = customBoard.Id;
        Name = customBoard.Name;
    }

    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsAll => (All()).Equals(this);

    public static IEnumerable<CustomBoardPinFilterItem> CreateFilterList()
    {
        return [All()];
    }

    private static CustomBoardPinFilterItem All()
    {
        return new CustomBoardPinFilterItem(ALL_PINS_ID, AppResources.AllPins);
    }
}
