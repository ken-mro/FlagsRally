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
/// Adds a custom board from the shared Google Drive folder, or from a file on the device.
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

    public ObservableCollection<DriveEntry> Entries { get; } = [];

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
                Entries.Add(entry);
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
        }
    }

    [RelayCommand]
    Task RetryAsync() => LoadAsync();

    [RelayCommand]
    async Task OpenEntryAsync(DriveEntry entry)
    {
        if (IsBusy || IsLoading) return;

        if (entry.IsFolder)
        {
            _openFolders.Add(entry);
            await LoadAsync();
            return;
        }

        try
        {
            IsBusy = true;
            using var stream = await _catalog.DownloadAsync(entry.Id);
            await ImportAsync(stream, entry.Name);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
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
        try
        {
            IsBusy = true;
            var pickedFile = await FilePicker.PickAsync();
            if (pickedFile is null) return;

            using var stream = await pickedFile.OpenReadAsync();
            await ImportAsync(stream, pickedFile.FileName);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Saves the board, asking first when a board with the same name is already there.
    async Task ImportAsync(Stream stream, string fileName)
    {
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
        WeakReferenceMessenger.Default.Send(new CustomBoardsChangedMessage());

        await Shell.Current.DisplayAlertAsync(AppResources.AddBoard, string.Format(AppResources.BoardAdded, board.name), "OK");
        await Shell.Current.GoToAsync("..");
    }
}
