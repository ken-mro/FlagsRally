using FlagsRally.Models.CustomBoard;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// How the places on a custom board are ordered.
/// </summary>
public enum LocationSort { Group, JsonOrder, NewestFirst, OldestFirst }

/// <summary>
/// A run of places shown together; only <see cref="LocationSort.Group"/> gives them a heading.
/// </summary>
public class CustomLocationGroup(string name, IEnumerable<CustomLocation> locations) : List<CustomLocation>(locations)
{
    public string Name { get; } = name;
    public bool HasHeading => !string.IsNullOrEmpty(Name);
    public string ProgressText => $"{this.Count(x => x.HasBeenVisited)} / {Count}";
}

public static class LocationSorter
{
    public static string DisplayName(this LocationSort sort) => sort switch
    {
        LocationSort.Group => AppResources.SortGroup,
        LocationSort.JsonOrder => AppResources.SortJsonOrder,
        LocationSort.OldestFirst => AppResources.SortOldest,
        _ => AppResources.SortNewest,
    };

    /// <summary>
    /// Orders the places; by check-in date the unvisited ones follow in their JSON order.
    /// </summary>
    public static List<CustomLocation> Sort(IEnumerable<CustomLocation> locations, LocationSort sort)
    {
        var byJson = locations.OrderBy(x => x.SortIndex).ToList();
        return sort switch
        {
            LocationSort.NewestFirst => [.. byJson.Where(x => x.HasBeenVisited).OrderByDescending(x => x.ArrivalDate), .. byJson.Where(x => x.HasNotBeenVisited)],
            LocationSort.OldestFirst => [.. byJson.Where(x => x.HasBeenVisited).OrderBy(x => x.ArrivalDate), .. byJson.Where(x => x.HasNotBeenVisited)],
            // Groups appear in the order they first occur in the JSON.
            LocationSort.Group => byJson.GroupBy(x => x.Group).SelectMany(g => g).ToList(),
            _ => byJson,
        };
    }

    public static List<CustomLocationGroup> Group(IEnumerable<CustomLocation> locations, LocationSort sort)
    {
        var sorted = Sort(locations, sort);
        if (sort != LocationSort.Group)
        {
            return [new CustomLocationGroup(string.Empty, sorted)];
        }
        return sorted.GroupBy(x => x.Group)
                     .Select(g => new CustomLocationGroup(string.IsNullOrWhiteSpace(g.Key) ? AppResources.OtherGroup : g.Key, g))
                     .ToList();
    }
}
