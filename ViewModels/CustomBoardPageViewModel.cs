using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Resources;
using FlagsRally.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace FlagsRally.ViewModels;

public partial class CustomBoardPageViewModel : BaseViewModel
{

    readonly CustomBoardService _customBoardService;
    public CustomBoardPageViewModel(CustomBoardService customBoardService)
    {
        Title = "Custom Board";

        _customBoardService = customBoardService;

        var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        using var stream1 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_1.json");
        using var stream2 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_2.json");
        using var stream3 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_3.json");

        SourceCustomLocationList = new ObservableCollection<CustomLocation>(GetResources([stream1!, stream2!, stream3!]));

        var groupBoard = SourceCustomLocationList.GroupBy(x => x.Board).Select(x => x.Key).ToList();
        CustomBoardList = new ObservableCollection<CustomBoard>(groupBoard);
        FilteredCustomBoard = SourceCustomLocationList.First().Board;
    }

    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayCustomLocationList))]
    ObservableCollection<CustomLocation> _sourceCustomLocationList = [];

    [ObservableProperty]
    ObservableCollection<CustomBoard> _customBoardList;


    CustomBoard _filteredCustomBoard = default!;
    public CustomBoard FilteredCustomBoard
    {
        get => _filteredCustomBoard;
        set
        {
            SetProperty(ref _filteredCustomBoard, value);
            OnPropertyChanged(nameof(DisplayCustomLocationList));
            _ = Init();
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

    private IEnumerable<CustomLocation> GetResources(Stream[] streamList)
    {
        var sourceList = new List<CustomLocation>();
        foreach(var stream in streamList)
        {
            if (stream == null) continue;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var customBoard = JsonSerializer.Deserialize<CustomBoardJson>(json) ?? new();
            sourceList.AddRange(_customBoardService.GetCustomLocations(customBoard));
        }

        return sourceList.AsEnumerable();
    }

    private async Task Init()
    {
        try
        {
            IsBusy = true;
            if (FilteredCustomBoard == null) return;

            //var subRegionList = await _arrivalLocationDataRepository.GetSubRegionsByCountryCode(_filteredCountry.CountryShortCode);
            //SourceArrivalSubRegionList = new ObservableCollection<SubRegion>(subRegionList);
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

    private ObservableCollection<CustomLocation> GetFilteredList()
    {
        var filteredList = SourceCustomLocationList.Where(x => x.Board == FilteredCustomBoard)
                            .OrderByDescending(x => x.ArrivalDate).ToList();
        return new ObservableCollection<CustomLocation>(filteredList);
        //var countryInfo = CountryList.First(x => x.CountryShortCode == FilteredCountry.CountryShortCode);
        //var twoLetterRegionName = _settingsPreferences.GetCountryOfResidence();
        //List<SubRegion> blankAllSubregionList = _subRegionHelper.GetBlankAllRegionList(countryInfo, twoLetterRegionName);

        ////Assign the arrival data to the blankAllSubregionList.
        //foreach (var SourceArrivalSubRegion in SourceArrivalSubRegionList)
        //{
        //    var subRegionCode = SourceArrivalSubRegion?.Code;

        //    if (string.IsNullOrEmpty(subRegionCode?.RegionCode))
        //    {
        //        //Try to get code and save it if acquired.
        //        var acquiredSubRegionCodeString = _customCountryHelper.GetAdminAreaCode(countryInfo.CountryShortCode, SourceArrivalSubRegion?.EnAdminAreaName ?? string.Empty);
        //        if (string.IsNullOrEmpty(acquiredSubRegionCodeString)) continue;

        //        _arrivalLocationDataRepository.UpdateAdminAreaCode(SourceArrivalSubRegion?.Id ?? 0, acquiredSubRegionCodeString);
        //        subRegionCode = new SubRegionCode(countryInfo.CountryShortCode, acquiredSubRegionCodeString);
        //    }

        //    var blankInstance = blankAllSubregionList.Find(x => x.Code.lowerCountryCodeHyphenRegionCode == subRegionCode?.lowerCountryCodeHyphenRegionCode);
        //    if (blankInstance is null) continue;

        //    if (blankInstance.ArrivalDate < SourceArrivalSubRegion.ArrivalDate)
        //    {
        //        blankInstance.ArrivalDate = SourceArrivalSubRegion.ArrivalDate;
        //    }
        //}

        //return new ObservableCollection<SubRegion>(blankAllSubregionList.OrderByDescending(x => x.ArrivalDate).ToList());
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
