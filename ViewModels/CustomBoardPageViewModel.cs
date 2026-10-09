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
    readonly SettingsPreferences _settingsPreferences;
    public CustomBoardPageViewModel(CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, MapFocusRequest mapFocusRequest, SettingsPreferences settingsPreferences)
    {
        _mapFocusRequest = mapFocusRequest;
        _settingsPreferences = settingsPreferences;
        _selectedSort = Enum.TryParse<LocationSort>(settingsPreferences.GetCustomBoardSort(nameof(LocationSort.NewestFirst)), out var sort) ? sort : LocationSort.NewestFirst;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationGroups))]
    [NotifyPropertyChangedFor(nameof(VisitedCount))]
    [NotifyPropertyChangedFor(nameof(TotalCount))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    ObservableCollection<CustomLocation> _sourceCustomLocationList = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationGroups))]
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationGroups))]
    [NotifyPropertyChangedFor(nameof(SortText))]
    LocationSort _selectedSort;

    partial void OnSelectedSortChanged(LocationSort value) => _settingsPreferences.SetCustomBoardSort(value.ToString());

    public string SortText => SelectedSort.DisplayName();

    [RelayCommand]
    async Task ChooseSortAsync()
    {
        // The sort in use carries a tick, since the toolbar only shows an icon.
        var sorts = Enum.GetValues<LocationSort>();
        string Label(LocationSort sort) => sort == SelectedSort ? $"✓ {sort.DisplayName()}" : sort.DisplayName();
        var choice = await Shell.Current.DisplayActionSheetAsync(AppResources.SortBy, AppResources.Cancel, null, sorts.Select(Label).ToArray());
        SelectedSort = sorts.FirstOrDefault(x => Label(x) == choice, SelectedSort);
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
            OnPropertyChanged(nameof(DisplayCustomLocationGroups));
            OnPropertyChanged(nameof(VisitedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateIsVisible))]
    bool _dateIsNotVisible;

    public bool DateIsVisible => !DateIsNotVisible;

    public ObservableCollection<CustomLocation> DisplayCustomLocationList => new(LocationSorter.Sort(FilteredLocations, SelectedSort));

    public List<CustomLocationGroup> DisplayCustomLocationGroups => LocationSorter.Group(FilteredLocations, SelectedSort);


    [ObservableProperty]
    bool _isRefreshing = false;

    public async Task Init()
    {
        try
        {
            IsBusy = true;

            var allCustomLocations = await _customLocationDataRepository.GetAllCustomLocations();
            // Set the field and let the FilteredCustomBoard assignment below raise every change once,
            // so the grouped list is built a single time per visit.
#pragma warning disable MVVMTK0034
            _sourceCustomLocationList = new ObservableCollection<CustomLocation>(allCustomLocations);
#pragma warning restore MVVMTK0034
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

    IEnumerable<CustomLocation> FilteredLocations => BoardLocations.Where(x => SelectedVisitFilter.Matches(x.HasBeenVisited));

    [RelayCommand]
    public async Task RefreshCountriesAsync()
    {
        IsRefreshing = true;
        await Init();
        IsRefreshing = false;
    }

    // "Hide date and month" as a small menu, ticked when on.
    [RelayCommand]
    async Task ChooseDisplayOptionsAsync()
    {
        var hideDate = DateIsNotVisible ? $"✓ {AppResources.HideDateAndMonth}" : AppResources.HideDateAndMonth;
        var choice = await Shell.Current.DisplayActionSheetAsync(AppResources.DisplayOptions, AppResources.Cancel, null, hideDate);
        if (choice == hideDate)
        {
            DateIsNotVisible = !DateIsNotVisible;
        }
    }
}
