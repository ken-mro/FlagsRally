using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Helpers;
using System.Collections.ObjectModel;
using FlagsRally.Resources;
using FlagsRally.Utilities;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BaseViewModel
{
    private readonly SubRegionHelper _subRegionHelper;
    private readonly IArrivalLocationDataRepository _arrivalLocationDataRepository;
    private readonly SettingsPreferences _settingsPreferences;
    private readonly CustomCountryHelper _customCountryHelper;


    public FlagsBoardPageViewModel(IArrivalLocationDataRepository arrivalLocationDataRepository, SubRegionHelper arrivalInfoService, SettingsPreferences settingsPreferences, CustomCountryHelper customCountryHelper)
    {
        Title = "Flags Board";

        _arrivalLocationDataRepository = arrivalLocationDataRepository;
        _subRegionHelper = arrivalInfoService;
        _settingsPreferences = settingsPreferences;
        _customCountryHelper = customCountryHelper;

        CountryList = new ObservableCollection<Country>()
        {
            customCountryHelper.GetCountryByCode("IT"),
            customCountryHelper.GetCountryByCode("DE"),
            customCountryHelper.GetCountryByCode("JP"),
            customCountryHelper.GetCountryByCode("US"),
        };

        var regionName = _settingsPreferences.GetCountryOrRegion();
        var matchingCountry = CountryList.FirstOrDefault(c => c.CountryShortCode == regionName);
        if (matchingCountry != null)
        {
            CountryList.Remove(matchingCountry);
            CountryList.Insert(0, matchingCountry);
        }

        FilteredCountry = CountryList.First();

        _ = Init();
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFullSubRegionList))]
    ObservableCollection<SubRegion> _sourceArrivalSubRegionList = [];

    [ObservableProperty]
    ObservableCollection<Country> _countryList;

    Country _filteredCountry;
    public Country FilteredCountry
    {
        get => _filteredCountry;
        set
        {
            SetProperty(ref _filteredCountry, value);
            _ = Init();
        }
    }

    [ObservableProperty]
    bool _isSettingsVisible;

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

            var subRegionList = await _arrivalLocationDataRepository.GetSubRegionsByCountryCode(_filteredCountry.CountryShortCode);
            SourceArrivalSubRegionList = new ObservableCollection<SubRegion>(subRegionList);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ObservableCollection<SubRegion> GetFilteredList()
    {
        var countryInfo = CountryList.First(x => x.CountryShortCode == FilteredCountry.CountryShortCode);

        List<SubRegion> blankAllSubregionList;
        if (countryInfo.CountryShortCode == "US")
        {
            blankAllSubregionList = countryInfo.Regions
                .Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }.Contains(x.ShortCode))
                .OrderBy(x => x.ShortCode).Select(x => new SubRegion
                {
                    Name = x.Name,
                    Code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode)
                }).ToList();
        }
        else
        {
            var regionName = _settingsPreferences.GetCountryOrRegion();
            var isCountryOfResidence = countryInfo.CountryShortCode.Equals(regionName);
            var isSupported = _subRegionHelper.isSupported(regionName.ToUpper());

            blankAllSubregionList = countryInfo.Regions
            .OrderBy(x => x.ShortCode).Select(x =>
            {
                var code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode);
                var name = _subRegionHelper.GetLocalSubregionName(code);
                return new SubRegion
                {
                    Name = isCountryOfResidence && isSupported ? 
                           _subRegionHelper.GetLocalSubregionName(code) : x.Name,
                    Code = code
                };
            }).ToList();
        }

        foreach (var SourceArrivalSubRegion in SourceArrivalSubRegionList)
        {
            var blankInstance = blankAllSubregionList.Find(x => x.Code.lower5LetterRegionCode == SourceArrivalSubRegion.Code.lower5LetterRegionCode);
            if (blankInstance is null) throw new NullReferenceException("Fail to get blank SubRegion list by SubRegion Code");

            if (string.IsNullOrEmpty(blankInstance.ArrivalDate.ToString()))
            {
                blankInstance.ArrivalDate = SourceArrivalSubRegion.ArrivalDate;
                continue;
            }

            if (blankInstance.ArrivalDate < SourceArrivalSubRegion.ArrivalDate)
            {
                blankInstance.ArrivalDate = SourceArrivalSubRegion.ArrivalDate;
            }
        }

        return new ObservableCollection<SubRegion>(blankAllSubregionList.OrderByDescending(x => x.ArrivalDate).ToList());
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
