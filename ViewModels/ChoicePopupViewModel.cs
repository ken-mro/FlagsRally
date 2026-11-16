using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FlagsRally.ViewModels;

/// <summary>
/// One row of a <see cref="ChoicePopupViewModel"/>: its text and whether it is ticked. An option
/// shows a tick box, on or off; one of several to pick shows a tick only when it is the one in use.
/// </summary>
public partial class ChoiceRow(int index, string label, bool isTicked, bool isOption, bool isLast, ICommand tapCommand) : ObservableObject
{
    public int Index { get; } = index;
    public string Label { get; } = label;
    public bool IsOption { get; } = isOption;

    /// <summary>A line under every row but the last.</summary>
    public bool HasDivider { get; } = !isLast;

    public ICommand TapCommand { get; } = tapCommand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsTick))]
    bool _isTicked = isTicked;

    public bool ShowsTick => !IsOption && IsTicked;
}

/// <summary>
/// A short list in the app's own popup, in place of the system action sheet. Either one row is
/// picked (the popup closes and <see cref="PickedIndex"/> says which; the row in use carries a
/// tick), or the rows are options switched on and off, each change applied at once.
/// </summary>
public partial class ChoicePopupViewModel : BaseViewModel
{
    /// <summary>Above this many rows the list is drawn row by row as it scrolls (all the countries).</summary>
    public const int LongListRows = 12;

    readonly Action<int, bool>? _switched;

    ChoicePopupViewModel(string title, IReadOnlyList<(string Label, bool IsTicked)> rows, Action<int, bool>? switched)
    {
        Heading = title;
        _switched = switched;
        Rows = [.. rows.Select((row, index) =>
            new ChoiceRow(index, row.Label, row.IsTicked, switched is not null, index == rows.Count - 1, TapRowCommand))];
    }

    /// <summary>Pick one of <paramref name="labels"/>; <paramref name="current"/> is the one in use, if any.</summary>
    public static ChoicePopupViewModel PickOne(string title, IReadOnlyList<string> labels, int? current = null) =>
        new(title, [.. labels.Select((label, index) => (label, index == current))], null);

    /// <summary>Options that are on or off; <paramref name="switched"/> gets the row and its new state.</summary>
    public static ChoicePopupViewModel Options(string title, IReadOnlyList<(string Label, bool IsOn)> options, Action<int, bool> switched) =>
        new(title, options, switched);

    public Popup? Popup { get; set; }

    public string Heading { get; }
    public IReadOnlyList<ChoiceRow> Rows { get; }

    public bool IsOptions => _switched is not null;
    public bool IsPickOne => !IsOptions;

    public bool IsLong => Rows.Count > LongListRows;
    public bool IsShort => !IsLong;

    /// <summary>The row picked, or null when the popup was closed without picking.</summary>
    public int? PickedIndex { get; private set; }

    [RelayCommand]
    async Task TapRowAsync(ChoiceRow row)
    {
        if (_switched is not null)
        {
            row.IsTicked = !row.IsTicked;
            _switched(row.Index, row.IsTicked);
            return;
        }

        PickedIndex = row.Index;
        await CloseAsync();
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
