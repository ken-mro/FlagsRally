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

public partial class CustomBoardPageViewModel : BaseViewModel, IQueryAttributable, IVisitFilterable
{
    public const string BoardQueryKey = "board";

    // Board requested by the Collections page; applied on the next Init().
    string? _requestedBoardName;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(BoardQueryKey, out var board))
        {
            _requestedBoardName = board as string;
        }
    }


    readonly ICustomBoardRepository _customBoardRepository;
    readonly ICustomLocationDataRepository _customLocationDataRepository;
    readonly MapFocusRequest _mapFocusRequest;
    public CustomBoardPageViewModel(CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, MapFocusRequest mapFocusRequest)
    {
        _mapFocusRequest = mapFocusRequest;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(VisitedCount))]
    [NotifyPropertyChangedFor(nameof(TotalCount))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    ObservableCollection<CustomLocation> _sourceCustomLocationList = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(ShowsAll))]
    [NotifyPropertyChangedFor(nameof(ShowsVisited))]
    [NotifyPropertyChangedFor(nameof(ShowsUnvisited))]
    VisitFilter _selectedVisitFilter = VisitFilter.All;

    public bool ShowsAll => SelectedVisitFilter == VisitFilter.All;
    public bool ShowsVisited => SelectedVisitFilter == VisitFilter.Visited;
    public bool ShowsUnvisited => SelectedVisitFilter == VisitFilter.Unvisited;

    IEnumerable<CustomLocation> BoardLocations => SourceCustomLocationList.Where(x => x.Board.Name == FilteredCustomBoard?.Name);

    public int VisitedCount => BoardLocations.Count(x => x.HasBeenVisited);
    public int TotalCount => BoardLocations.Count();
    public double Progress => TotalCount == 0 ? 0 : (double)VisitedCount / TotalCount;
    public string ProgressText => $"{VisitedCount} / {TotalCount}";

    [RelayCommand]
    void SetVisitFilter(string filter)
    {
        SelectedVisitFilter = Enum.Parse<VisitFilter>(filter);
    }

    [RelayCommand]
    async Task ShowOnMapAsync(CustomLocation location)
    {
        _mapFocusRequest.Request(location.CompositeKey);
        await Shell.Current.GoToAsync("//Map");
    }

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
            OnPropertyChanged(nameof(VisitedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
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
            var selectedBoardName = _requestedBoardName ?? FilteredCustomBoard?.Name ?? latestCustomLocation?.Board.Name;
            _requestedBoardName = null;
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
        var filteredList = BoardLocations
                            .Where(x => SelectedVisitFilter.Matches(x.HasBeenVisited))
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
    void ChangeSettingsVisibility()
    {
        IsSettingsVisible = !IsSettingsVisible;
    }
}
