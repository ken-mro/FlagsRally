using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Messages;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

/// <summary>
/// A card on the Collections page: a custom board with its progress, or the "Add board" card.
/// </summary>
public record CollectionCard(string Title, int Visited, int Total, string ImageUrl, bool IsAddCard = false)
{
    public bool IsBoard => !IsAddCard;
    public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
    public bool HasNoImage => IsBoard && !HasImage;
    public double Progress => Total == 0 ? 0 : (double)Visited / Total;
    public string ProgressText => $"{Visited} / {Total}";
    public string Initial => string.IsNullOrEmpty(Title) ? string.Empty : Title[..1];
}

public partial class CollectionsPageViewModel : BaseViewModel
{
    readonly ICustomBoardRepository _customBoardRepository;
    readonly ICustomLocationDataRepository _customLocationDataRepository;
    readonly CustomBoardService _customBoardService;

    public CollectionsPageViewModel(ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, CustomBoardService customBoardService)
    {
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _customBoardService = customBoardService;
    }

    public ObservableCollection<CollectionCard> BoardCards { get; } = [];

    public async Task Init()
    {
        try
        {
            var boards = await _customBoardRepository.GetAllCustomBoards();
            var locationsByBoard = (await _customLocationDataRepository.GetAllCustomLocations())
                .GroupBy(l => l.Board.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            BoardCards.Clear();
            foreach (var board in boards)
            {
                var locations = locationsByBoard.GetValueOrDefault(board.Name) ?? [];
                var latestVisit = locations.Where(l => l.HasBeenVisited).MaxBy(l => l.ArrivalDate);
                BoardCards.Add(new CollectionCard(
                    board.Name,
                    locations.Count(l => l.HasBeenVisited),
                    locations.Count,
                    latestVisit?.ImageUrl ?? string.Empty));
            }
            BoardCards.Add(new CollectionCard(AppResources.AddBoard, 0, 0, string.Empty, IsAddCard: true));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task OpenRegionalFlagsAsync() => await Shell.Current.GoToAsync(FlagsBoardPage.Route);

    [RelayCommand]
    async Task EditBoardsAsync() => await Shell.Current.GoToAsync(ManageCustomBoardsPage.Route);

    [RelayCommand]
    async Task OpenCardAsync(CollectionCard card)
    {
        if (card.IsAddCard)
        {
            await AddBoardAsync();
            return;
        }

        await Shell.Current.GoToAsync(CustomBoardPage.Route, new ShellNavigationQueryParameters
        {
            { CustomBoardPageViewModel.BoardQueryKey, card.Title },
        });
    }

    private async Task AddBoardAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var pickedFile = await FilePicker.PickAsync();
            if (pickedFile is null) return;

            using var stream = await pickedFile.OpenReadAsync();
            await _customBoardService.SaveBoardAndLocations(stream, pickedFile.FileName);

            WeakReferenceMessenger.Default.Send(new CustomBoardsChangedMessage());
            await Init();
        }
        catch (OperationCanceledException)
        {
            // User cancelled password entry
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
}
