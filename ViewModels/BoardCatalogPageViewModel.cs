using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Messages;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

/// <summary>
/// A file or folder in the board catalog; board files can be selected to add several at once.
/// </summary>
public partial class BoardCatalogItem(DriveEntry entry) : ObservableObject
{
    public DriveEntry Entry { get; } = entry;

    public string DisplayName => Entry.DisplayName;
    public bool IsFolder => Entry.IsFolder;
    public bool IsBoardFile => Entry.IsBoardFile;

    [ObservableProperty]
    bool _isSelected;
}

/// <summary>
/// Adds custom boards from the shared Google Drive folder, or from files on the device.
/// </summary>
public partial class BoardCatalogPageViewModel : BaseViewModel
{
    readonly DriveBoardCatalog _catalog;
    readonly CustomBoardService _customBoardService;
    readonly ICustomBoardRepository _customBoardRepository;

    // Folders opened below the root, innermost last.
    readonly List<DriveEntry> _openFolders = [];
    bool _isLoaded;

    public BoardCatalogPageViewModel(DriveBoardCatalog catalog, CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository)
    {
        _catalog = catalog;
        _customBoardService = customBoardService;
        _customBoardRepository = customBoardRepository;
    }

    public ObservableCollection<BoardCatalogItem> Entries { get; } = [];

    // Boards chosen so far, in the order they were chosen. Kept while moving between folders,
    // so boards from different folders can be added together.
    readonly List<DriveEntry> _selected = [];

    public int SelectedCount => _selected.Count;
    public bool HasSelection => _selected.Count > 0;
    public string AddSelectedText => string.Format(AppResources.AddSelectedBoards, _selected.Count);

    public bool HasBoardFiles => Entries.Any(x => x.IsBoardFile);
    public bool AllBoardsSelected => Entries.Where(x => x.IsBoardFile).All(x => x.IsSelected);
    public string SelectAllText => HasBoardFiles && AllBoardsSelected ? AppResources.ClearBoardSelection : AppResources.SelectAllBoards;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEmptyFolder))]
    bool _hasLoadError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEmptyFolder))]
    bool _isLoading;

    public bool ShowsEmptyFolder => !IsLoading && !HasLoadError && Entries.Count == 0;

    public bool IsInSubfolder => _openFolders.Count > 0;

    public string FolderTitle => _openFolders.LastOrDefault()?.Name ?? AppResources.SharedBoards;

    public async Task Init()
    {
        if (_isLoaded) return;
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        OnPropertyChanged(nameof(IsInSubfolder));
        OnPropertyChanged(nameof(FolderTitle));
        Entries.Clear();
        HasLoadError = false;
        IsLoading = true;
        try
        {
            var folderId = _openFolders.LastOrDefault()?.Id ?? DriveBoardCatalog.RootFolderId;
            foreach (var entry in (await _catalog.ListAsync(folderId)).Where(x => x.IsFolder || x.IsBoardFile))
            {
                Entries.Add(new BoardCatalogItem(entry) { IsSelected = _selected.Any(x => x.Id == entry.Id) });
            }
            _isLoaded = true;
        }
        catch (Exception)
        {
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
            OnSelectionChanged();
        }
    }

    [RelayCommand]
    Task RetryAsync() => LoadAsync();

    void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AddSelectedText));
        OnPropertyChanged(nameof(HasBoardFiles));
        OnPropertyChanged(nameof(AllBoardsSelected));
        OnPropertyChanged(nameof(SelectAllText));
    }

    void SetSelected(BoardCatalogItem item, bool isSelected)
    {
        item.IsSelected = isSelected;
        _selected.RemoveAll(x => x.Id == item.Entry.Id);
        if (isSelected) _selected.Add(item.Entry);
    }

    [RelayCommand]
    void ToggleSelectAll()
    {
        var select = !AllBoardsSelected;
        foreach (var item in Entries.Where(x => x.IsBoardFile))
        {
            SetSelected(item, select);
        }
        OnSelectionChanged();
    }

    // A folder opens; a board is selected or unselected.
    [RelayCommand]
    async Task OpenEntryAsync(BoardCatalogItem item)
    {
        if (IsBusy || IsLoading) return;

        if (item.IsFolder)
        {
            _openFolders.Add(item.Entry);
            await LoadAsync();
            return;
        }

        SetSelected(item, !item.IsSelected);
        OnSelectionChanged();
    }

    [RelayCommand]
    async Task AddSelectedAsync()
    {
        if (IsBusy || _selected.Count == 0) return;

        var results = new ImportResults();
        try
        {
            IsBusy = true;
            foreach (var entry in _selected.ToList())
            {
                await ImportOneAsync(results, entry.DisplayName, async () => await _catalog.DownloadAsync(entry.Id), entry.Name);
            }
        }
        finally
        {
            IsBusy = false;
        }

        await FinishImportAsync(results);
    }

    /// <summary>
    /// Goes up one folder; returns false at the root so the page itself can close.
    /// </summary>
    public bool GoUp()
    {
        if (!IsInSubfolder || IsLoading) return IsInSubfolder;
        _openFolders.RemoveAt(_openFolders.Count - 1);
        _ = LoadAsync();
        return true;
    }

    [RelayCommand]
    async Task NavigateBackAsync()
    {
        if (!GoUp())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    async Task AddFromDeviceAsync()
    {
        if (IsBusy) return;

        IEnumerable<FileResult?> pickedFiles;
        try
        {
            pickedFiles = await FilePicker.PickMultipleAsync() ?? [];
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
            return;
        }

        var files = pickedFiles.OfType<FileResult>().ToList();
        if (files.Count == 0) return;

        var results = new ImportResults();
        try
        {
            IsBusy = true;
            foreach (var file in files)
            {
                await ImportOneAsync(results, file.FileName, file.OpenReadAsync, file.FileName);
            }
        }
        finally
        {
            IsBusy = false;
        }

        await FinishImportAsync(results);
    }

    sealed class ImportResults
    {
        public List<string> Added { get; } = [];
        public List<string> Failed { get; } = [];
    }

    // Saves one board, asking first when a board with the same name is already there.
    // A failure is noted and the next board is tried.
    async Task ImportOneAsync(ImportResults results, string label, Func<Task<Stream>> open, string fileName)
    {
        try
        {
            using var stream = await open();
            var board = await _customBoardService.ReadBoardJsonAsync(stream, fileName);

            var existingBoards = await _customBoardRepository.GetAllCustomBoards();
            if (existingBoards.Any(x => x.Name == board.name))
            {
                var replaces = await Shell.Current.DisplayAlertAsync(
                    AppResources.Confirmation,
                    string.Format(AppResources.ConfirmReplaceBoard, board.name),
                    AppResources.Yes,
                    AppResources.No);
                if (!replaces) return;
            }

            await _customBoardService.SaveBoardAndLocations(board);
            results.Added.Add(board.name);
        }
        catch (Exception ex)
        {
            results.Failed.Add($"{label} ({ex.Message})");
        }
    }

    // One message for the whole batch; the page closes once something was added.
    async Task FinishImportAsync(ImportResults results)
    {
        if (results.Added.Count > 0)
        {
            WeakReferenceMessenger.Default.Send(new CustomBoardsChangedMessage());
        }

        var lines = new List<string>();
        if (results.Added.Count == 1 && results.Failed.Count == 0)
        {
            lines.Add(string.Format(AppResources.BoardAdded, results.Added[0]));
        }
        else if (results.Added.Count > 0)
        {
            lines.Add(string.Format(AppResources.BoardsAdded, string.Join("\n", results.Added.Select(x => $"・{x}"))));
        }
        if (results.Failed.Count > 0)
        {
            lines.Add(string.Format(AppResources.BoardsNotAdded, string.Join("\n", results.Failed.Select(x => $"・{x}"))));
            lines.Add(AppResources.PleaseTryAgain);
        }
        if (lines.Count == 0) return;

        var title = results.Added.Count > 0 ? AppResources.AddBoard : AppResources.Error;
        await Shell.Current.DisplayAlertAsync(title, string.Join("\n\n", lines), "OK");

        if (results.Added.Count > 0)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
