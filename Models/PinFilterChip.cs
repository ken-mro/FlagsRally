using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Models;

/// <summary>
/// One filter chip on the map: "All pins", "Arrival location", a custom board, or a series of
/// boards (such as the manhole card issues) that opens a list to choose from.
/// </summary>
public partial class PinFilterChip : ObservableObject
{
    public PinFilterChip(CustomBoardPinFilterItem item)
    {
        Name = item.Name;
        IsAll = item.IsAll;
        Members = item.Members;
    }

    public string Name { get; }
    public bool IsAll { get; }

    /// <summary>The pin keys (board names) the chip stands for.</summary>
    public IReadOnlyList<string> Members { get; }

    public bool IsGroup => Members.Count > 1;

    [ObservableProperty]
    bool _isSelected;

    /// <summary>How many of the members are shown, for a series.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    int _selectedCount;

    /// <summary>
    /// A series shows how many of its boards are chosen and a ▾ for its list, e.g. "マンホールカード 3/31 ▾".
    /// The no-break spaces keep the count and the ▾ with the name.
    /// </summary>
    public string Text => !IsGroup ? Name
        : SelectedCount > 0 ? $"{Name} {SelectedCount}/{Members.Count} ▾"
        : $"{Name} ▾";
}

public static class PinFilterRules
{
    /// <param name="visibleKeys">Pin keys currently shown; empty means all pins.</param>
    public static List<PinFilterChip> Build(IEnumerable<CustomBoardPinFilterItem> items, IReadOnlyCollection<string> visibleKeys)
    {
        var chips = items.Select(item => new PinFilterChip(item)).ToList();
        Refresh(chips, visibleKeys);
        return chips;
    }

    /// <summary>
    /// Marks the chips for the shown keys: "All pins" when nothing is chosen, otherwise every chip
    /// with a chosen member.
    /// </summary>
    public static void Refresh(IEnumerable<PinFilterChip> chips, IReadOnlyCollection<string> visibleKeys)
    {
        foreach (var chip in chips)
        {
            chip.SelectedCount = chip.IsAll ? 0 : chip.Members.Count(visibleKeys.Contains);
            chip.IsSelected = chip.IsAll ? visibleKeys.Count == 0 : chip.SelectedCount > 0;
        }
    }

    /// <summary>
    /// Applies a tap on a chip that is not a series: "All pins" clears the choice, any other chip
    /// toggles itself. Nothing chosen means all pins. Returns the keys to show.
    /// </summary>
    public static IReadOnlyCollection<string> Toggle(IReadOnlyList<PinFilterChip> chips, PinFilterChip tapped, IReadOnlyCollection<string> visibleKeys)
    {
        var keys = tapped.IsAll ? [] : SetShown(visibleKeys, tapped.Members, !tapped.IsSelected);
        Refresh(chips, keys);
        return keys;
    }

    /// <summary>
    /// Shows or hides some members (boards of a series chosen in its list). Returns the keys to show.
    /// </summary>
    public static IReadOnlyCollection<string> SetShown(IReadOnlyCollection<string> visibleKeys, IEnumerable<string> members, bool shown)
    {
        var keys = new List<string>(visibleKeys);
        foreach (var member in members)
        {
            keys.Remove(member);
            if (shown) keys.Add(member);
        }
        return keys;
    }
}
