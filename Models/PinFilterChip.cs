using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Models;

/// <summary>
/// One filter chip on the map: "All pins", "Arrival location" or a custom board.
/// </summary>
public partial class PinFilterChip : ObservableObject
{
    public PinFilterChip(CustomBoardPinFilterItem item, bool isSelected)
    {
        Name = item.Name;
        IsAll = item.IsAll;
        _isSelected = isSelected;
    }

    public string Name { get; }
    public bool IsAll { get; }

    [ObservableProperty]
    bool _isSelected;
}

public static class PinFilterRules
{
    /// <param name="visibleKeys">Pin keys currently shown; empty means all pins.</param>
    public static List<PinFilterChip> Build(IEnumerable<CustomBoardPinFilterItem> items, IReadOnlyCollection<string> visibleKeys)
    {
        return items
            .Select(item => new PinFilterChip(item, item.IsAll ? visibleKeys.Count == 0 : visibleKeys.Contains(item.Name)))
            .ToList();
    }

    /// <summary>
    /// Applies a tap on <paramref name="tapped"/>: "All pins" clears the other chips,
    /// any other chip toggles itself, and nothing selected falls back to "All pins".
    /// Returns the pin keys to show (empty means all pins).
    /// </summary>
    public static IReadOnlyCollection<string> Toggle(IReadOnlyList<PinFilterChip> chips, PinFilterChip tapped)
    {
        if (tapped.IsAll)
        {
            foreach (var chip in chips)
            {
                chip.IsSelected = chip.IsAll;
            }
            return [];
        }

        tapped.IsSelected = !tapped.IsSelected;

        var selected = chips.Where(c => !c.IsAll && c.IsSelected).Select(c => c.Name).ToList();
        foreach (var chip in chips.Where(c => c.IsAll))
        {
            chip.IsSelected = selected.Count == 0;
        }
        return selected;
    }
}
