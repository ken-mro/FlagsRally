using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Services;
using System.Collections.ObjectModel;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BaseViewModel, IQueryAttributable, IVisitFilterable
{
    public const string CountryQueryKey = "country";

    private readonly RegionalFlagsService _regionalFlagsService;

    public FlagsBoardPageViewModel(RegionalFlagsService regionalFlagsService)
    {
        Title = "Flags Board";

        _regionalFlagsService = regionalFlagsService;
        FilteredCountry = _regionalFlagsService.GetCountries().First();
    }

    // Opened from a country card on the Collections page; loaded by the next Init().
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(CountryQueryKey, out var code) && code is string countryCode)
        {
            FilteredCountry = _regionalFlagsService.GetCountry(countryCode);
        }
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFullSubRegionList))]
    [NotifyPropertyChangedFor(nameof(VisitedCount))]
    [NotifyPropertyChangedFor(nameof(TotalCount))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    ObservableCollection<SubRegion> _sourceArrivalSubRegionList = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFullSubRegionList))]
    [NotifyPropertyChangedFor(nameof(ShowsAll))]
    [NotifyPropertyChangedFor(nameof(ShowsVisited))]
    [NotifyPropertyChangedFor(nameof(ShowsUnvisited))]
    VisitFilter _selectedVisitFilter = VisitFilter.All;

    public bool ShowsAll => SelectedVisitFilter == VisitFilter.All;
    public bool ShowsVisited => SelectedVisitFilter == VisitFilter.Visited;
    public bool ShowsUnvisited => SelectedVisitFilter == VisitFilter.Unvisited;

    public int VisitedCount => SourceArrivalSubRegionList.Count(x => x.HasBeenVisited);
    public int TotalCount => SourceArrivalSubRegionList.Count;
    public double Progress => TotalCount == 0 ? 0 : (double)VisitedCount / TotalCount;
    public string ProgressText => $"{VisitedCount} / {TotalCount}";

    [RelayCommand]
    void SetVisitFilter(string filter)
    {
        SelectedVisitFilter = Enum.Parse<VisitFilter>(filter);
    }

    private bool _isLoaded = false;
    public string ShapesSource => GetShapesSource();

    private string _mapCountryShortCode = string.Empty;
    public string GetShapesSource()
    {
        if (!_isLoaded) return string.Empty;
        _mapCountryShortCode = FilteredCountry?.CountryShortCode.ToLower() ?? string.Empty;
        return $"{Constants.GEOJSON_RESOURCE_BASE_URL}/{_mapCountryShortCode}.json";
    }

    [ObservableProperty]
    Country? _filteredCountry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListVisible))]
    bool _isMapVisible;

    public bool IsListVisible => !IsMapVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateIsVisible))]
    bool _dateIsNotVisible;

    public bool DateIsVisible => !DateIsNotVisible;

    public ObservableCollection<SubRegion> DisplayFullSubRegionList =>
        new(SourceArrivalSubRegionList.Where(x => SelectedVisitFilter.Matches(x.HasBeenVisited)));

    [ObservableProperty]
    bool _isRefreshing = false;

    public async Task Init()
    {
        try
        {
            IsBusy = true;
            if (FilteredCountry == null) return;

            var regions = await _regionalFlagsService.GetRegionsAsync(FilteredCountry);
            SourceArrivalSubRegionList = new ObservableCollection<SubRegion>(regions);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
        }
        finally
        {
            if (!_isLoaded) _isLoaded = true;
            IsBusy = false;
        }
    }

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

    [RelayCommand]
    async Task ChangeMapVisibilityAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            IsMapVisible = !IsMapVisible;
            if (!IsMapVisible) return;

            if (!FilteredCountry!.CountryShortCode.ToLower().Equals(_mapCountryShortCode))
            {
                // To Show the activity indicator before executing OnPropertyChanged
                await Task.Delay(100);
                OnPropertyChanged(nameof(ShapesSource));
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
}
