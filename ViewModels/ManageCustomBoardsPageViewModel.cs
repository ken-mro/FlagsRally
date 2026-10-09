using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Messages;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FlagsRally.ViewModels;

public partial class ManageableBoardItem : ObservableObject
{
    public ManageableBoardItem(string name)
    {
        Name = name;
    }

    public string Name { get; }

    [ObservableProperty]
    bool _isSelected;
}

public partial class ManageCustomBoardsPageViewModel : BaseViewModel
{
    readonly ICustomBoardRepository _customBoardRepository;
    readonly CustomBoardService _customBoardService;
    readonly AppShell _appShell;

    public ManageCustomBoardsPageViewModel(ICustomBoardRepository customBoardRepository, CustomBoardService customBoardService, AppShell appShell)
    {
        _customBoardRepository = customBoardRepository;
        _customBoardService = customBoardService;
        _appShell = appShell;
        Title = AppResources.ManageBoards;
    }

    public ObservableCollection<ManageableBoardItem> Boards { get; } = [];

    public int SelectedCount => Boards.Count(x => x.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    public string DeleteSelectedText => string.Format(AppResources.DeleteSelected, SelectedCount);

    public async Task Init()
    {
        try
        {
            IsBusy = true;
            var allBoards = await _customBoardRepository.GetAllCustomBoards();
            foreach (var item in Boards)
            {
                item.PropertyChanged -= OnBoardItemPropertyChanged;
            }
            Boards.Clear();
            foreach (var board in allBoards)
            {
                var item = new ManageableBoardItem(board.Name);
                item.PropertyChanged += OnBoardItemPropertyChanged;
                Boards.Add(item);
            }
            NotifySelectionChanged();
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

    private void OnBoardItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ManageableBoardItem.IsSelected))
        {
            NotifySelectionChanged();
        }
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(DeleteSelectedText));
    }

    [RelayCommand]
    async Task MoveUpAsync(ManageableBoardItem item) => await MoveAsync(item, -1);

    [RelayCommand]
    async Task MoveDownAsync(ManageableBoardItem item) => await MoveAsync(item, 1);

    private async Task MoveAsync(ManageableBoardItem item, int offset)
    {
        var oldIndex = Boards.IndexOf(item);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= Boards.Count) return;

        Boards.Move(oldIndex, newIndex);

        try
        {
            await _customBoardRepository.UpdateSortOrdersAsync(Boards.Select(x => x.Name).ToList());
            WeakReferenceMessenger.Default.Send(new CustomBoardsChangedMessage());
        }
        catch (Exception ex)
        {
            Boards.Move(newIndex, oldIndex);
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
    }

    [RelayCommand]
    async Task DeleteSelectedAsync()
    {
        var selectedNames = Boards.Where(x => x.IsSelected).Select(x => x.Name).ToList();
        if (selectedNames.Count == 0 || IsBusy) return;

        var message = $"{AppResources.ConfirmDeleteBoards}\n\n{string.Join("\n", selectedNames)}\n\n{AppResources.ActionCannotUndone}";
        var deletes = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", message, $"{AppResources.Yes}", $"{AppResources.No}");
        if (!deletes) return;

        try
        {
            IsBusy = true;
            await _customBoardService.DeleteBoardsAsync(selectedNames);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
        finally
        {
            IsBusy = false;
            // Notify even after a partial failure so other pages drop what was actually removed.
            WeakReferenceMessenger.Default.Send(new CustomBoardsChangedMessage());
        }

        if (!await _customBoardRepository.GetCustomBoardExists())
        {
            // The Custom Board tab is about to be hidden; leave it before hiding it.
            await Shell.Current.GoToAsync("//FlagsBoard");
            await _appShell.SetCustomBoardPageVisibility();
            return;
        }

        await Init();
    }
}
