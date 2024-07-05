using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Helpers;
using System.Collections.ObjectModel;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using System.Diagnostics;
using System.Linq;

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

        var countryList = new List<Country>();
        foreach (var country in Constants.SupportedSubRegionCountryCodeList)
        {
            countryList.Add(customCountryHelper.GetCountryByCode(country.ToUpper()));
        }

        CountryList = new ObservableCollection<Country>(countryList.OrderBy(x => x.CountryName).ToList());

        var latestCountryCode = _settingsPreferences.GetLatestCountry();
        var countryCodeOfResidence = _settingsPreferences.GetCountryOfResidence();
        Country? matchingCountry = CountryList.FirstOrDefault(c => c.CountryShortCode == latestCountryCode)
                            ?? CountryList.FirstOrDefault(c => c.CountryShortCode == countryCodeOfResidence);
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
        var twoLetterRegionName = _settingsPreferences.GetCountryOfResidence();
        List<SubRegion> blankAllSubregionList = _subRegionHelper.GetBlankAllRegionList(countryInfo, twoLetterRegionName);

        //Assign the arrival data to the blankAllSubregionList.
        foreach (var SourceArrivalSubRegion in SourceArrivalSubRegionList)
        {
            var subRegionCode = SourceArrivalSubRegion?.Code;

            if (string.IsNullOrEmpty(subRegionCode?.RegionCode))
            {
                //Try to get code and save it if acquired.
                var acquiredSubRegionCodeString = _customCountryHelper.GetAdminAreaCode(countryInfo.CountryShortCode, SourceArrivalSubRegion?.EnAdminAreaName ?? string.Empty);
                if (string.IsNullOrEmpty(acquiredSubRegionCodeString)) continue;

                _arrivalLocationDataRepository.UpdateAdminAreaCode(SourceArrivalSubRegion?.Id ?? 0, acquiredSubRegionCodeString);
                subRegionCode = new SubRegionCode(countryInfo.CountryShortCode, acquiredSubRegionCodeString);
            }

            var blankInstance = blankAllSubregionList.Find(x => x.Code.lowerCountryCodeHyphenRegionCode == subRegionCode?.lowerCountryCodeHyphenRegionCode);
            if (blankInstance is null) continue;

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
