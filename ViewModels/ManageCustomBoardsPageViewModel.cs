using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Messages;
using FlagsRally.Repository;
using FlagsRally.Resources;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class ManageableBoardItem : ObservableObject
{
    public ManageableBoardItem(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public partial class ManageCustomBoardsPageViewModel : BaseViewModel
{
    readonly ICustomBoardRepository _customBoardRepository;

    public ManageCustomBoardsPageViewModel(ICustomBoardRepository customBoardRepository)
    {
        _customBoardRepository = customBoardRepository;
        Title = AppResources.ManageBoards;
    }

    public ObservableCollection<ManageableBoardItem> Boards { get; } = [];

    public async Task Init()
    {
        try
        {
            IsBusy = true;
            var allBoards = await _customBoardRepository.GetAllCustomBoards();
            Boards.Clear();
            foreach (var board in allBoards)
            {
                Boards.Add(new ManageableBoardItem(board.Name));
            }
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
            WeakReferenceMessenger.Default.Send(CustomBoardsChangedMessage.Reordered());
        }
        catch (Exception ex)
        {
            Boards.Move(newIndex, oldIndex);
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
    }
}
