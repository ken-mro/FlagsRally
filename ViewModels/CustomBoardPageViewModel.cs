using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Helpers;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace FlagsRally.ViewModels;

public partial class CustomBoardPageViewModel : BoardViewModelBase, IQueryAttributable
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
    readonly StampViewer _stampViewer;
    public CustomBoardPageViewModel(CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, MapFocusRequest mapFocusRequest, SettingsPreferences settingsPreferences, StampViewer stampViewer)
    {
        _stampViewer = stampViewer;
        _mapFocusRequest = mapFocusRequest;
        _settingsPreferences = settingsPreferences;
        _selectedSort = Enum.TryParse<LocationSort>(settingsPreferences.GetCustomBoardSort(nameof(LocationSort.NewestFirst)), out var sort) ? sort : LocationSort.NewestFirst;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
    }

    // A visited tile's image is sized from the page width and the board's image shape: left to size
    // itself, its height depended on the bitmap and the screen density and differed between devices.
    // Its width is fixed too: an image that sizes itself makes its tile, and the list, lay out again
    // when the stamp arrives, which made scrolling through visited places stutter.
    const double PagePadding = 5;

    double _pageWidth;
    bool _opened;

    [ObservableProperty]
    double _tileImageHeight = 180;

    [ObservableProperty]
    double _tileImageWidth = 180;

    public void UpdateTileSize(double pageWidth, bool opened = false)
    {
        if (pageWidth <= 0) return;
        _pageWidth = pageWidth;
        _opened = opened;
        GridItemSpan = GridSpan.For(pageWidth, opened);
        TileImageHeight = TileImageHeightFor(pageWidth, GridItemSpan, FilteredCustomBoard?.Width ?? 0, FilteredCustomBoard?.Height ?? 0);
        TileImageWidth = TileImageWidthFor(pageWidth, GridItemSpan);
    }

    /// <summary>
    /// The height of a tile's image: its column width scaled by the board's image shape (square when unknown).
    /// </summary>
    public static double TileImageHeightFor(double pageWidth, int span, double boardWidth, double boardHeight)
    {
        var ratio = boardWidth > 0 && boardHeight > 0 ? boardHeight / boardWidth : 1;
        return Math.Round(ImageColumnWidth(pageWidth, span) * ratio);
    }

    /// <summary>
    /// The width of a tile's image: the column's width, so the image keeps the board's shape at <see cref="TileImageHeightFor"/>.
    /// </summary>
    public static double TileImageWidthFor(double pageWidth, int span) => Math.Round(ImageColumnWidth(pageWidth, span));

    static double ImageColumnWidth(double pageWidth, int span) =>
        Math.Max((pageWidth - PagePadding * 2) / Math.Max(span, 1) - 4, 40); // minus the tile border

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationGroups))]
    ObservableCollection<CustomLocation> _sourceCustomLocationList = [];

    partial void OnSourceCustomLocationListChanged(ObservableCollection<CustomLocation> value) => OnCountsChanged();

    IEnumerable<CustomLocation> BoardLocations => SourceCustomLocationList.Where(x => x.Board.Name == FilteredCustomBoard?.Name);

    public override int VisitedCount => BoardLocations.Count(x => x.HasBeenVisited);
    public override int TotalCount => BoardLocations.Count();

    protected override void OnShownPlacesChanged()
    {
        OnPropertyChanged(nameof(DisplayCustomLocationList));
        OnPropertyChanged(nameof(DisplayCustomLocationGroups));
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
        var picked = await AppDialogs.PickOneAsync(AppResources.SortBy, [.. sorts.Select(sort => sort.DisplayName())], Array.IndexOf(sorts, SelectedSort));
        if (picked is int index)
        {
            SelectedSort = sorts[index];
        }
    }

    // A tile opens its place on the map, except a stamp still waiting to be pressed down (checked in
    // without a signal): it is pressed down right here, with its celebration, and the tile then shows it.
    [RelayCommand]
    async Task ShowOnMapAsync(CustomLocation location)
    {
        if (location.IsStampPending)
        {
            await _stampViewer.ShowAsync(location.CompositeKey, loading => IsBusy = loading);
            await Init();
            return;
        }
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
            UpdateTileSize(_pageWidth, _opened);
            OnShownPlacesChanged();
            OnCountsChanged();
        }
    }


    public ObservableCollection<CustomLocation> DisplayCustomLocationList => new(LocationSorter.Sort(FilteredLocations, SelectedSort));

    public List<CustomLocationGroup> DisplayCustomLocationGroups => LocationSorter.Group(FilteredLocations, SelectedSort);


    public override async Task Init()
    {
        await RunBusyAsync(async () =>
        {
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
        });
    }

    IEnumerable<CustomLocation> FilteredLocations => BoardLocations.Where(x => SelectedVisitFilter.Matches(x.HasBeenVisited));
}
