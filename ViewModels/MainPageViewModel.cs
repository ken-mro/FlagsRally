using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        private readonly SettingsPreferences _settingsPreferences;
        private readonly IArrivalInfoService _arrivalInfoService;
        private readonly CountryHelper _countryHelper;
        private const string ALL_COUNTRY_CODE = "All";
        private const string ALL_COUNTRY_NAME = "All Countries";

        public MainPageViewModel(SettingsPreferences settingPreferences, IArrivalInfoService arrivalInfoService)
        {
            Title = "Main Page";

            _settingsPreferences = settingPreferences;
            _settingsPreferences.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PassportImageSourceString));
            _arrivalInfoService = arrivalInfoService;
            _countryHelper = new CountryHelper();

            _ = Init();
        }

        [ObservableProperty]
        bool _isSettingsVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        bool _isUnique;

        [ObservableProperty]
        double _passportImageHeight;

        [ObservableProperty]
        int _gridItemSpan = 2;

        public string PassportImageSourceString => $"https://www.passportindex.org/countries/{_settingsPreferences.GetCountryOrRegion().ToLower()}.png";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        ObservableCollection<ArrivalLocation> _sourceArrivalLocationList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCountry), nameof(IsAdminArea))]
        string _selectedRegion = "Country";

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        Country _filteredCountry;

        public bool IsCountry => SelectedRegion == "Country";
        public bool IsAdminArea => SelectedRegion == "AdminArea";

        public ObservableCollection<ArrivalLocation> DisplayArrivalLocationList => GetFilteredList();

        [ObservableProperty]
        bool _isRefreshing = false;

        private async Task Init()
        {
            var sourceArrivalLocationList = new ObservableCollection<ArrivalLocation>(await _arrivalInfoService.GetAllCountries());
            SourceArrivalLocationList = sourceArrivalLocationList;

            var countryCodeList = sourceArrivalLocationList.Select(x => x.CountryCode).Distinct().ToList();

            var filteredCountryCode = FilteredCountry?.CountryShortCode ?? ALL_COUNTRY_CODE;
            var countryList = new List<Country>()
            {
                new Country()
                {
                    CountryName = ALL_COUNTRY_NAME,
                    CountryShortCode = ALL_COUNTRY_CODE
                }
            };

            countryList.AddRange(countryCodeList.ConvertAll(_countryHelper.GetCountryByCode).OrderBy(x => x.CountryName));

            CountryList = new ObservableCollection<Country>(countryList);
            FilteredCountry = CountryList.First(x => x.CountryShortCode == filteredCountryCode);
        }

        private ObservableCollection<ArrivalLocation> GetFilteredList()
        {
            try
            {
            if (SourceArrivalLocationList is null)
                return new ObservableCollection<ArrivalLocation>();

            IEnumerable<ArrivalLocation> arrivalLocationList;
            if (IsUnique && FilteredCountry.CountryShortCode == ALL_COUNTRY_CODE)
            {
                var reversedList = SourceArrivalLocationList.Reverse();
                arrivalLocationList = reversedList.GroupBy(x => x.CountryCode).Select(x => x.First());
            }
            else
            {
                arrivalLocationList = SourceArrivalLocationList.Reverse();
            }

            if (string.IsNullOrEmpty(FilteredCountry?.CountryShortCode) || FilteredCountry.CountryShortCode == ALL_COUNTRY_CODE)
                return new ObservableCollection<ArrivalLocation>(arrivalLocationList.ToList());

            return new ObservableCollection<ArrivalLocation>(arrivalLocationList.Where(x => x.CountryCode == FilteredCountry.CountryShortCode).ToList());
        }
            catch (Exception ex)
            {
                //To handle when FilteredCountry is null.
            }

            return new ObservableCollection<ArrivalLocation>();
        }

        [RelayCommand]
        void ChangeSettingsVisibility()
        {
            IsSettingsVisible = !IsSettingsVisible;
        }

        [RelayCommand]
        public async Task RefreshCountriesAsync()
        {
            IsRefreshing = true;
            await Init();
            IsRefreshing = false;
        }

        [RelayCommand]
        public async Task ChangeRegionAsync()
        {
            if (SelectedRegion == "Country")
            {
                SelectedRegion = "AdminArea";
            }
            else
            {
                SelectedRegion = "Country";
            }
        }
    }
}