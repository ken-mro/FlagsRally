using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace FlagsRally.ViewModels;

public partial class CustomBoardPageViewModel : BaseViewModel
{

    readonly ICustomBoardRepository _customBoardRepository;
    readonly ICustomLocationDataRepository _customLocationDataRepository;
    public CustomBoardPageViewModel(CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository)
    {
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    ObservableCollection<CustomLocation> _sourceCustomLocationList = [];

    [ObservableProperty]
    ObservableCollection<CustomBoard> _customBoardList = default!;


    CustomBoard _filteredCustomBoard = default!;
    public CustomBoard FilteredCustomBoard
    {
        get => _filteredCustomBoard;
        set
        {
            SetProperty(ref _filteredCustomBoard, value);
            OnPropertyChanged(nameof(DisplayCustomLocationList));
        }
    }

    [ObservableProperty]
    bool _isSettingsVisible;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateIsVisible))]
    bool _dateIsNotVisible;

    public bool DateIsVisible => !DateIsNotVisible;

    public ObservableCollection<CustomLocation> DisplayCustomLocationList => GetFilteredList();


    [ObservableProperty]
    bool _isRefreshing = false;

    public async Task Init()
    {
        try
        {
            IsBusy = true;

            var allCustomLocations = await _customLocationDataRepository.GetAllCustomLocations();
            SourceCustomLocationList = new ObservableCollection<CustomLocation>(allCustomLocations);
            var latestCustomLocation = allCustomLocations.MaxBy(x => x.ArrivalDate);

            var allBoards = await _customBoardRepository.GetAllCustomBoards();
            CustomBoardList = new ObservableCollection<CustomBoard>(allBoards);
            // Keep the current selection (matched by name, since the order may have changed);
            // otherwise default to the board visited most recently.
            var selectedBoardName = FilteredCustomBoard?.Name ?? latestCustomLocation?.Board.Name;
            var matchingBoard = allBoards.FirstOrDefault(x => x.Name.Equals(selectedBoardName));
            FilteredCustomBoard = (matchingBoard ?? allBoards.FirstOrDefault())!;
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

    private ObservableCollection<CustomLocation> GetFilteredList()
    {
        var filteredList = SourceCustomLocationList.Where(x => x.Board.Name == FilteredCustomBoard?.Name)
                            .OrderByDescending(x => x.ArrivalDate).ToList();
        return new ObservableCollection<CustomLocation>(filteredList);
    }

    [RelayCommand]
    public async Task RefreshCountriesAsync()
    {
        IsRefreshing = true;
        await Init();
        IsRefreshing = false;
    }

    [RelayCommand]
    async Task OpenManageBoardsAsync()
    {
        await Shell.Current.GoToAsync(ManageCustomBoardsPage.Route);
    }

    [RelayCommand]
    void ChangeSettingsVisibility()
    {
        IsSettingsVisible = !IsSettingsVisible;
    }
}
