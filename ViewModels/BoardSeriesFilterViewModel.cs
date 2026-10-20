using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

/// <summary>
/// A board in a series' list on the map, ticked when its pins are shown.
/// </summary>
public partial class BoardSeriesFilterItem(string name, bool isShown) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    bool _isShown = isShown;
}

/// <summary>
/// A list of boards on the map, ticked when their pins are shown: the boards of a series chip
/// (such as マンホールカード), or every board and the arrival location from the Filter chip.
/// Select all, clear, or tick them one by one; each change applies to the map at once.
/// </summary>
public partial class BoardSeriesFilterViewModel : ObservableObject
{
    readonly Action<IReadOnlyList<string>, bool> _setShown;

    /// <param name="setShown">Shows (true) or hides the given boards' pins.</param>
    public BoardSeriesFilterViewModel(string title, IEnumerable<BoardSeriesFilterItem> boards, Action<IReadOnlyList<string>, bool> setShown)
    {
        Title = title;
        Boards = boards.ToList();
        _setShown = setShown;
    }

    public Popup? Popup { get; set; }

    public string Title { get; }

    public IReadOnlyList<BoardSeriesFilterItem> Boards { get; }

    [RelayCommand]
    void ToggleBoard(BoardSeriesFilterItem board)
    {
        board.IsShown = !board.IsShown;
        _setShown([board.Name], board.IsShown);
    }

    // Both are always there: with some boards ticked, either one takes a single tap.
    [RelayCommand]
    void ShowAll() => SetAll(true);

    [RelayCommand]
    void HideAll() => SetAll(false);

    void SetAll(bool show)
    {
        foreach (var board in Boards)
        {
            board.IsShown = show;
        }
        _setShown(Boards.Select(b => b.Name).ToList(), show);
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
