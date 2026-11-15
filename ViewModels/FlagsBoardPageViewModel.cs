using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Services;
using System.Collections.ObjectModel;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BoardViewModelBase, IQueryAttributable
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
    [NotifyPropertyChangedFor(nameof(DisplayFullSubRegionList))]
    ObservableCollection<SubRegion> _sourceArrivalSubRegionList = [];

    partial void OnSourceArrivalSubRegionListChanged(ObservableCollection<SubRegion> value) => OnCountsChanged();

    public override int VisitedCount => SourceArrivalSubRegionList.Count(x => x.HasBeenVisited);
    public override int TotalCount => SourceArrivalSubRegionList.Count;

    protected override void OnShownPlacesChanged() => OnPropertyChanged(nameof(DisplayFullSubRegionList));

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

    public ObservableCollection<SubRegion> DisplayFullSubRegionList =>
        new(SourceArrivalSubRegionList.Where(x => SelectedVisitFilter.Matches(x.HasBeenVisited)));

    public override async Task Init()
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
