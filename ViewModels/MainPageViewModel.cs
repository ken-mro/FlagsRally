using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        private readonly SettingsPreferences _settingsPreferences;
        private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
        private readonly CustomCountryHelper _countryHelper;
        private readonly ArrivalLocationService _arrivalLocationService;
        private const string ALL_COUNTRY_CODE = "All";
        private readonly string ALL_COUNTRY_NAME = AppResources.AllCountries;

        public MainPageViewModel(CustomCountryHelper countryHelper, SettingsPreferences settingPreferences, IArrivalLocationDataRepository arrivalLocationRepository, ArrivalLocationService arrivalLocationService)
        {
            Title = "Main Page";

            _settingsPreferences = settingPreferences;
            _settingsPreferences.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PassportImageSourceString));
            _arrivalLocationRepository = arrivalLocationRepository;
            _countryHelper = countryHelper;
            _arrivalLocationService = arrivalLocationService;

            _ = Init();
        }

        [ObservableProperty]
        bool _isSettingsVisible;

        [ObservableProperty]
        bool _isMapVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DateIsVisible))]
        bool _dateIsNotVisible;
 
        public bool DateIsVisible => !DateIsNotVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        bool _isUnique;

        [ObservableProperty]
        double _passportImageHeight;

        [ObservableProperty]
        int _gridItemSpan = 2;

        public string PassportImageSourceString => $"https://www.passportindex.org/countries/{_settingsPreferences.GetCountryOfResidence().ToLower()}.png";


        public ObservableCollection<ArrivalLocation> MapArrivalLocationList
            => GetMapArrivalLocationList();

        private ObservableCollection<ArrivalLocation> GetMapArrivalLocationList()
        {
            var result = new List<ArrivalLocation>();
            result.AddRange(_allCountries);
            result.AddRange(GetDistinctArrivalLocationList());
            return new ObservableCollection<ArrivalLocation>(result);
        }

        private ObservableCollection<ArrivalLocation> GetDistinctArrivalLocationList()
        {
            if (DisplayArrivalLocationList is null) return [];
            var arrivalLocations = DisplayArrivalLocationList?.GroupBy(x => x.CountryCode)
                .Select(x => x.FirstOrDefault())
                .ToList();
            return new ObservableCollection<ArrivalLocation>(arrivalLocations!);
        }


        private bool _isMapInitialized = false;
        private IEnumerable<ArrivalLocation> _allCountries = [];
        public string ShapesSource => GetShapesSource();
        public string GetShapesSource()
        {
            if (!_isMapInitialized) return string.Empty;

            _allCountries = _arrivalLocationService.GetAllCountriesArrivalLocations();
            var arrivedAq = SourceArrivalLocationList.Where(l => l.CountryCode.ToLower().Equals("aq")).Any();
            if (arrivedAq) return $"{Constants.GeoJsonResourceBaseUrl}/world-map.json";
            return $"{Constants.GeoJsonResourceBaseUrl}/non-aq-world-map.json";
        }

        [ObservableProperty]
        ObservableCollection<ArrivalLocation> _sourceArrivalLocationList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCountry), nameof(IsAdminArea))]
        string _selectedRegion = AppResources.Country;

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        [NotifyPropertyChangedFor(nameof(MapArrivalLocationList))]
        Country _filteredCountry;

        public bool IsCountry => SelectedRegion == AppResources.Country;
        public bool IsAdminArea => SelectedRegion == AppResources.AdminArea;

        public ObservableCollection<ArrivalLocation> DisplayArrivalLocationList => GetFilteredList();

        [ObservableProperty]
        bool _isRefreshing = false;

        private async Task Init()
        {
            try
            {
                IsBusy = true;
                var allArrivalLocationList = await _arrivalLocationRepository.GetAllArrivalLocations();
                var sourceArrivalLocationList = new ObservableCollection<ArrivalLocation>(allArrivalLocationList);
                SourceArrivalLocationList = sourceArrivalLocationList;

                var distinctArrivalLocationList = sourceArrivalLocationList.GroupBy(x => x.CountryCode).Select(x => x.FirstOrDefault()).ToList();
                var arrivalCountryList = distinctArrivalLocationList.ConvertAll(x => new Country()
                {
                    CountryName = x.CountryName,
                    CountryShortCode = x.CountryCode,
                });

                var filteredCountryCode = FilteredCountry?.CountryShortCode ?? ALL_COUNTRY_CODE;
                var countryList = new List<Country>()
                {
                    new Country()
                    {
                        CountryName = ALL_COUNTRY_NAME,
                        CountryShortCode = ALL_COUNTRY_CODE
                    }
                };

                countryList.AddRange(arrivalCountryList.OrderBy(x => x.CountryName).ToList());

                CountryList = new ObservableCollection<Country>(countryList);
                FilteredCountry = CountryList.First(x => x.CountryShortCode == filteredCountryCode);
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
            if (SelectedRegion == AppResources.Country)
            {
                SelectedRegion = AppResources.AdminArea;
            }
            else
            {
                SelectedRegion = AppResources.Country;
            }
        }

        [RelayCommand]
        async Task DeleteArrivalLocationAsync(ArrivalLocation arrivalLocation)
        {
            try
            {
                IsBusy = true;
                await _arrivalLocationRepository.DeleteAsync(arrivalLocation.Id);
                await Init();
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        async Task ShowLocationInfo(ArrivalLocation arrivalLocation)
        {
            var longitude = arrivalLocation.Location.Longitude;
            var roundedLongitude = Math.Round(longitude, 3);
            var latitude = arrivalLocation.Location.Latitude;
            var roundedLatitude = Math.Round(latitude, 3);

            await Shell.Current.DisplayAlert($"{AppResources.ArrivalLocationInfo}", $"\n{AppResources.Date}: {arrivalLocation.ArrivalDate}\n" +
                                                                    $"{AppResources.Country}: {arrivalLocation.CountryName}\n" +
                                                                    $"{AppResources.AdminArea}: {arrivalLocation.AdminAreaName}\n" +
                                                                    $"{AppResources.Locality}: {arrivalLocation.LocalityName}\n" +
                                                                    $"{AppResources.Location}: {roundedLatitude}, {roundedLongitude}","OK");
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
                if (_isMapInitialized) return;

                _isMapInitialized = true;
                // To Show the activity indicator before executing OnPropertyChanged
                await Task.Delay(100);

                OnPropertyChanged(nameof(ShapesSource));
                OnPropertyChanged(nameof(MapArrivalLocationList));
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
    }
}