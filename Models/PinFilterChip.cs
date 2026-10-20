using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models.CustomBoard;

namespace FlagsRally.Models;

/// <summary>
/// One filter chip on the map: "Arrival location", a custom board, or a series of boards (such as
/// the manhole card issues) that opens a list to choose from. A chip is selected while its pins are shown.
/// </summary>
public partial class PinFilterChip : ObservableObject
{
    public PinFilterChip(CustomBoardPinFilterItem item)
    {
        Name = item.Name;
        Members = item.Members;
    }

    public string Name { get; }

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
    /// A series shows a ▾ for its list, and how many of its boards are shown when only some are,
    /// e.g. "マンホールカード 3/31 ▾". The no-break spaces keep the count and the ▾ with the name.
    /// </summary>
    public string Text => !IsGroup ? Name
        : SelectedCount > 0 && SelectedCount < Members.Count ? $"{Name}\u00A0{SelectedCount}/{Members.Count}\u00A0▾"
        : $"{Name}\u00A0▾";
}

public static class PinFilterRules
{
    /// <param name="hiddenKeys">Pin keys hidden from the map; empty means all pins are shown.</param>
    public static List<PinFilterChip> Build(IEnumerable<CustomBoardPinFilterItem> items, IReadOnlyCollection<string> hiddenKeys)
    {
        var chips = items.Select(item => new PinFilterChip(item)).ToList();
        Refresh(chips, hiddenKeys);
        return chips;
    }

    /// <summary>Every pin key the chips stand for (arrival location and every board).</summary>
    public static IReadOnlyList<string> AllKeys(IEnumerable<PinFilterChip> chips) =>
        chips.SelectMany(c => c.Members).Distinct().ToList();

    /// <summary>Selects every chip with a member shown, and counts a series' shown boards.</summary>
    public static void Refresh(IEnumerable<PinFilterChip> chips, IReadOnlyCollection<string> hiddenKeys)
    {
        foreach (var chip in chips)
        {
            chip.SelectedCount = chip.Members.Count(m => !hiddenKeys.Contains(m));
            chip.IsSelected = chip.SelectedCount > 0;
        }
    }

    /// <summary>
    /// Applies a tap on a chip that is not a series: it shows its pins when they are hidden and
    /// hides them when they are shown. Returns the keys to hide.
    /// </summary>
    public static IReadOnlyCollection<string> Toggle(IReadOnlyList<PinFilterChip> chips, PinFilterChip tapped, IReadOnlyCollection<string> hiddenKeys)
    {
        var keys = SetShown(hiddenKeys, tapped.Members, shown: !tapped.IsSelected);
        Refresh(chips, keys);
        return keys;
    }

    /// <summary>
    /// Shows or hides some keys (ticked or unticked in a list). Returns the keys to hide.
    /// </summary>
    public static IReadOnlyCollection<string> SetShown(IReadOnlyCollection<string> hiddenKeys, IEnumerable<string> keys, bool shown)
    {
        var hidden = new List<string>(hiddenKeys);
        foreach (var key in keys)
        {
            hidden.Remove(key);
            if (!shown) hidden.Add(key);
        }
        return hidden;
    }
}
