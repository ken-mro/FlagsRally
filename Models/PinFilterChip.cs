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
    /// <param name="hiddenKeys">Pin keys hidden from the map; empty means all pins are shown.</param>
    public static List<PinFilterChip> Build(IEnumerable<CustomBoardPinFilterItem> items, IReadOnlyCollection<string> hiddenKeys)
    {
        var chips = items.Select(item => new PinFilterChip(item)).ToList();
        Refresh(chips, hiddenKeys);
        return chips;
    }

    /// <summary>Every pin key the chips stand for (arrival location and every board).</summary>
    public static IReadOnlyList<string> AllKeys(IEnumerable<PinFilterChip> chips) =>
        chips.Where(c => !c.IsAll).SelectMany(c => c.Members).Distinct().ToList();

    /// <summary>
    /// Marks the chips: "All pins" when nothing is hidden, otherwise every chip with a member shown.
    /// </summary>
    public static void Refresh(IEnumerable<PinFilterChip> chips, IReadOnlyCollection<string> hiddenKeys)
    {
        var filtering = hiddenKeys.Count > 0;
        foreach (var chip in chips)
        {
            chip.SelectedCount = chip.IsAll || !filtering ? 0 : chip.Members.Count(m => !hiddenKeys.Contains(m));
            chip.IsSelected = chip.IsAll ? !filtering : chip.SelectedCount > 0;
        }
    }

    /// <summary>
    /// Applies a tap on a chip that is not a series. "All pins" shows everything. While everything
    /// is shown, a chip shows only its own pins; otherwise it shows or hides its pins. Hiding the
    /// last shown chip this way goes back to all pins. Returns the keys to hide.
    /// </summary>
    public static IReadOnlyCollection<string> Toggle(IReadOnlyList<PinFilterChip> chips, PinFilterChip tapped, IReadOnlyCollection<string> hiddenKeys)
    {
        IReadOnlyCollection<string> keys;
        var allKeys = AllKeys(chips);
        if (tapped.IsAll)
        {
            keys = [];
        }
        else if (hiddenKeys.Count == 0)
        {
            keys = allKeys.Except(tapped.Members).ToList();
        }
        else
        {
            keys = SetShown(hiddenKeys, tapped.Members, shown: !tapped.IsSelected);
            if (allKeys.All(keys.Contains)) keys = [];
        }
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
