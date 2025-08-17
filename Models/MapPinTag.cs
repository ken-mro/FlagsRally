using FlagsRally.Resources;

namespace FlagsRally.Models;

public class MapPinTag
{
    public int ArrivalLocationId { get; init; } = 0;
    public string CustomLocationKey { get; init; } = string.Empty;
    public string BoardName { get; init; } = AppResources.ArrivalLocation;
    public string PinKey => BoardName;
    public bool IsVisited { get; set; } = true;
    public bool IsCustomLocation => !string.IsNullOrEmpty(CustomLocationKey);
    public bool IsArrivalLocation => ArrivalLocationId > 0;

    private MapPinTag(int arrivalLocationId)
    {
        ArrivalLocationId = arrivalLocationId;
    }

    private MapPinTag(string key, string boardName, bool isVisited)
    {
        CustomLocationKey = key;
        BoardName = boardName;
        IsVisited = isVisited;
    }

    public static MapPinTag SetCustomLocationTag(string key, string boardName, bool isVisited)
    {
        return new MapPinTag(key, boardName, isVisited);
    }

    public static MapPinTag SetArrivalLocationTag(int id)
    {
        return new MapPinTag(id);
    }
}
