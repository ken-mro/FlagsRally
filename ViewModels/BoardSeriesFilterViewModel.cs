using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Resources;

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
/// The list a series chip opens on the map (such as マンホールカード): show the whole series or
/// tick the boards one by one. Each change applies to the map at once.
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

    public bool AllShown => Boards.All(b => b.IsShown);

    public string ToggleAllText => AllShown ? AppResources.ClearBoardSelection : AppResources.SelectAllBoards;

    [RelayCommand]
    void ToggleBoard(BoardSeriesFilterItem board)
    {
        board.IsShown = !board.IsShown;
        _setShown([board.Name], board.IsShown);
        OnPropertyChanged(nameof(AllShown));
        OnPropertyChanged(nameof(ToggleAllText));
    }

    [RelayCommand]
    void ToggleAll()
    {
        var show = !AllShown;
        foreach (var board in Boards)
        {
            board.IsShown = show;
        }
        _setShown(Boards.Select(b => b.Name).ToList(), show);
        OnPropertyChanged(nameof(AllShown));
        OnPropertyChanged(nameof(ToggleAllText));
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
