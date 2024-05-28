using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Helpers;
using System.Collections.ObjectModel;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BaseViewModel
{
    private readonly SubRegionHelper _subRegionHelper;
    private readonly IArrivalLocationDataRepository _arrivalLocationDataRepository;
    private readonly SettingsPreferences _settingsPreferences;


    public FlagsBoardPageViewModel(IArrivalLocationDataRepository arrivalLocationDataRepository, SubRegionHelper arrivalInfoService, SettingsPreferences settingsPreferences)
    {
        Title = "Flags Board";

        _arrivalLocationDataRepository = arrivalLocationDataRepository;
        _subRegionHelper = arrivalInfoService;
        _settingsPreferences = settingsPreferences;

        var countryHelper = new CountryHelper();
        CountryList = new ObservableCollection<Country>()
        {
            countryHelper.GetCountryByCode("JP"),
            countryHelper.GetCountryByCode("US"),
        };

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
                .Where(x => !new[] { "AA", "AE", "AP", "AS", "DC", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }.Contains(x.ShortCode))
                .OrderBy(x => x.ShortCode).Select(x => new SubRegion
                {
                Name = x.Name,
                Code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode)
            }).ToList();
        }
        else if (countryInfo.CountryShortCode == "JP")
        {
            var regionName = _settingsPreferences.GetCountryOrRegion();
            blankAllSubregionList = countryInfo.Regions
            .OrderBy(x => x.ShortCode).Select(x => 
            {
                var code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode);
                return new SubRegion
                {
                    Name = regionName == "JP" ? _subRegionHelper.GetJaSubregionName(code) : x.Name,
                    Code = code
                };
            }).ToList();
        }
        else
        {
            blankAllSubregionList = countryInfo.Regions
            .OrderBy(x => x.ShortCode).Select(x => new SubRegion
            {
                Name = x.Name,
                Code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode)
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
