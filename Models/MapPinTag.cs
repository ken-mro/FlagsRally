using FlagsRally.Resources;

namespace FlagsRally.Models;

public class MapPinTag
{
    public string CustomLocationKey { get; init; } = string.Empty;
    public string BoardName { get; init; } = AppResources.ArrivalLocation;
    public string PinKey => BoardName;
    public bool IsVisited { get; set; } = true;
    public bool IsCustomLocation => !string.IsNullOrEmpty(CustomLocationKey);

    public MapPinTag()
    {
    }

    public MapPinTag(string key, string boardName, bool isVisited)
    {
        CustomLocationKey = key;
        BoardName = boardName;
        IsVisited = isVisited;
    }
}
