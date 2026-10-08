using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Services;
using System.Collections.ObjectModel;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BaseViewModel, IQueryAttributable
{
    public const string CountryQueryKey = "country";

    private readonly RegionalFlagsService _regionalFlagsService;

    public FlagsBoardPageViewModel(RegionalFlagsService regionalFlagsService)
    {
        Title = "Flags Board";

        _regionalFlagsService = regionalFlagsService;

        CountryList = new ObservableCollection<Country>(_regionalFlagsService.GetCountries());
        FilteredCountry = CountryList.First();
    }

    // Opened from a country card on the Collections page.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(CountryQueryKey, out var code) && code is string countryCode)
        {
            FilteredCountry = CountryList.FirstOrDefault(x => x.CountryShortCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase)) ?? FilteredCountry;
        }
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFullSubRegionList))]
    ObservableCollection<SubRegion> _sourceArrivalSubRegionList = [];

    [ObservableProperty]
    ObservableCollection<Country> _countryList;

    private bool _isLoaded = false;
    public string ShapesSource => GetShapesSource();

    private string _mapCountryShortCode = string.Empty;
    public string GetShapesSource()
    {
        if (!_isLoaded) return string.Empty;
        _mapCountryShortCode = FilteredCountry?.CountryShortCode.ToLower() ?? string.Empty;
        return $"{Constants.GEOJSON_RESOURCE_BASE_URL}/{_mapCountryShortCode}.json";
    }

    Country? _filteredCountry ;
    public Country? FilteredCountry
    {
        get => _filteredCountry;
        set
        {
            SetProperty(ref _filteredCountry, value);

            if (IsMapVisible)
            {
                OnPropertyChanged(nameof(ShapesSource));
            }

            _ = Init();
        }
    }

    [ObservableProperty]
    bool _isSettingsVisible;

    [ObservableProperty]
    bool _isMapVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateIsVisible))]
    bool _dateIsNotVisible;

    public bool DateIsVisible => !DateIsNotVisible;

    public ObservableCollection<SubRegion> DisplayFullSubRegionList => GetFilteredList();
    

    [ObservableProperty]
    bool _isRefreshing = false;

    private async Task Init()
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

    private ObservableCollection<SubRegion> GetFilteredList() => new(SourceArrivalSubRegionList);

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
