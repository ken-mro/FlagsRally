using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;

namespace FlagsRally.ViewModels;

/// <summary>
/// A list of boards on the map, ticked when their pins are shown: the boards of a series chip
/// (such as マンホールカード), or every board and the arrival location from the Filter chip.
/// Boards of one series sit in a group that opens like a folder in the Windows features dialog;
/// its tick box shows whether all, some or none of them are shown, and a tap on it shows or hides
/// them all. Select all, clear, or tick them one by one; each change applies to the map at once.
/// </summary>
public partial class BoardSeriesFilterViewModel : ObservableObject
{
    readonly IReadOnlyList<PinFilterNode> _roots;
    readonly Func<IReadOnlyList<string>, bool, IReadOnlyCollection<string>> _setShown;

    /// <param name="roots">The outermost rows; groups start closed.</param>
    /// <param name="hiddenKeys">The pin keys hidden now.</param>
    /// <param name="setShown">Shows (true) or hides the given pin keys and returns the keys hidden after that.</param>
    public BoardSeriesFilterViewModel(
        string title,
        IReadOnlyList<PinFilterNode> roots,
        IReadOnlyCollection<string> hiddenKeys,
        Func<IReadOnlyList<string>, bool, IReadOnlyCollection<string>> setShown)
    {
        Title = title;
        _roots = roots;
        _setShown = setShown;
        PinFilterTree.Refresh(_roots, hiddenKeys);
        Rows = new ObservableCollection<PinFilterNode>(PinFilterTree.Visible(_roots));
    }

    public Popup? Popup { get; set; }

    public string Title { get; }

    /// <summary>The rows on screen: the outermost ones and those inside opened groups.</summary>
    public ObservableCollection<PinFilterNode> Rows { get; }

    /// <summary>A board's row, or a group's tick box: shows its boards unless all of them are shown.</summary>
    [RelayCommand]
    void ToggleShown(PinFilterNode row) => SetShown(row.Keys, PinFilterTree.ShowsOnTap(row));

    /// <summary>A row outside its tick box: a group opens or closes, a board is ticked or unticked.</summary>
    [RelayCommand]
    void TapRow(PinFilterNode row)
    {
        if (row.IsGroup) ToggleExpanded(row);
        else ToggleShown(row);
    }

    /// <summary>Opens or closes a group.</summary>
    [RelayCommand]
    void ToggleExpanded(PinFilterNode row)
    {
        if (!row.IsGroup) return;
        row.IsExpanded = !row.IsExpanded;

        // Only the rows under the group change, so the list is not rebuilt (that would scroll it to the top).
        var index = Rows.IndexOf(row);
        if (index < 0) return;
        if (row.IsExpanded)
        {
            foreach (var child in PinFilterTree.Visible(row.Children))
            {
                Rows.Insert(++index, child);
            }
        }
        else
        {
            while (index + 1 < Rows.Count && Rows[index + 1].Depth > row.Depth)
            {
                Rows.RemoveAt(index + 1);
            }
        }
    }

    // Both are always there: with some boards ticked, either one takes a single tap.
    [RelayCommand]
    void ShowAll() => SetShown(AllKeys, true);

    [RelayCommand]
    void HideAll() => SetShown(AllKeys, false);

    IReadOnlyList<string> AllKeys => _roots.SelectMany(r => r.Keys).ToList();

    void SetShown(IReadOnlyList<string> keys, bool show)
    {
        var hidden = _setShown(keys, show);
        PinFilterTree.Refresh(_roots, hidden);
    }

    [RelayCommand]
    async Task CloseAsync()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
