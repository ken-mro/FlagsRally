using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class FlagsBoardPageViewModel : BaseViewModel
{
    private readonly IArrivalInfoService _arrivalInfoService;


    public FlagsBoardPageViewModel(IArrivalInfoService arrivalInfoService)
    {
        Title = "Flags Board";

        _arrivalInfoService = arrivalInfoService;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceArrivalSubRegionList))]
    Country _filteredCountry;

    public ObservableCollection<SubRegion> DisplayFullSubRegionList => GetFilteredList();

    [ObservableProperty]
    bool _isRefreshing = false;

    private async Task Init()
    {
        try
        {
            if (FilteredCountry == null) return;

            SourceArrivalSubRegionList = new ObservableCollection<SubRegion>(await _arrivalInfoService.GetSubRegionsByCountryCode(_filteredCountry.CountryShortCode));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
        }

    }

    private ObservableCollection<SubRegion> GetFilteredList()
    {
        var countryInfo = CountryList.First(x => x.CountryShortCode == FilteredCountry.CountryShortCode);
        var blankAllSubregionList = countryInfo.Regions.OrderBy(x => x.ShortCode).Select(x => new SubRegion
        {
            Name = x.Name,
            Code = new SubRegionCode(FilteredCountry.CountryShortCode, x.ShortCode)
        }).ToList();

        foreach (var SourceArrivalSubRegion in SourceArrivalSubRegionList)
        {
            var blankInstance = blankAllSubregionList.Find(x => x.Code.lower5LetterRegionCode == SourceArrivalSubRegion.Code.lower5LetterRegionCode);
            if (blankInstance is null) throw new NullReferenceException("Fail to get blank SubRegion list by SubRegion Code");

            if (blankInstance.ArrivalDate is null)
            {
                blankInstance.ArrivalDate = SourceArrivalSubRegion.ArrivalDate;
                continue;
            }

            blankInstance.ArrivalDate = blankInstance.ArrivalDate ?? Math.Max(int.Parse(blankInstance.ArrivalDate), int.Parse(SourceArrivalSubRegion.ArrivalDate)).ToString();
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
}
