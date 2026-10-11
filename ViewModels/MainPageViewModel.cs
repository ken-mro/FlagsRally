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

        public MainPageViewModel(CustomCountryHelper countryHelper, SettingsPreferences settingPreferences, IArrivalLocationDataRepository arrivalLocationRepository, ArrivalLocationService arrivalLocationService, ICustomLocationDataRepository customLocationDataRepository)
        {
            Title = "Main Page";

            _settingsPreferences = settingPreferences;
            _settingsPreferences.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PassportImageSourceString));
            _arrivalLocationRepository = arrivalLocationRepository;
            _countryHelper = countryHelper;
            _arrivalLocationService = arrivalLocationService;
            _customLocationDataRepository = customLocationDataRepository;
            // Loaded by the page's OnAppearing.
        }

        readonly ICustomLocationDataRepository _customLocationDataRepository;

        // Summary shown next to the passport
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SummaryText))]
        int _visitedCountryCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SummaryText))]
        int _visitedRegionCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SummaryText))]
        int _checkInCount;

        public string SummaryText => string.Format(AppResources.PassportSummary, VisitedCountryCount, VisitedRegionCount, CheckInCount);

        // Set by the page while scrolling: the passport header is out of sight.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowsSummaryInTitle))]
        bool _isHeaderScrolledAway;

        public bool ShowsSummaryInTitle => IsHeaderScrolledAway || IsMapVisible;
        public bool IsListVisible => !IsMapVisible;
        public bool HasNoArrivals => SourceArrivalLocationList.Count == 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsListVisible))]
        [NotifyPropertyChangedFor(nameof(ShowsSummaryInTitle))]
        bool _isMapVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DateIsVisible))]
        bool _dateIsNotVisible;
 
        public bool DateIsVisible => !DateIsNotVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        bool _isUnique;

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

            _allCountries = _arrivalLocationService.GetAllCountriesArrivalLocationsForShapes();
            var arrivedAq = SourceArrivalLocationList.Where(l => l.CountryCode.ToLower().Equals("aq")).Any();
            if (arrivedAq) return $"{Constants.GEOJSON_RESOURCE_BASE_URL}/world-map.json";
            return $"{Constants.GEOJSON_RESOURCE_BASE_URL}/non-aq-world-map.json";
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoArrivals))]
        ObservableCollection<ArrivalLocation> _sourceArrivalLocationList = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCountry), nameof(IsAdminArea))]
        string _selectedRegion = AppResources.Country;

        [ObservableProperty]
        ObservableCollection<Country> _countryList = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
        [NotifyPropertyChangedFor(nameof(MapArrivalLocationList))]
        Country _filteredCountry = new ();

        public bool IsCountry => SelectedRegion == AppResources.Country;
        public bool IsAdminArea => SelectedRegion == AppResources.AdminArea;

        public ObservableCollection<ArrivalLocation> DisplayArrivalLocationList => GetFilteredList();

        [ObservableProperty]
        bool _isRefreshing = false;

        public async Task Init()
        {
            try
            {
                IsBusy = true;
                var allArrivalLocationList = await _arrivalLocationRepository.GetAllArrivalLocations();
                var sourceArrivalLocationList = new ObservableCollection<ArrivalLocation>(allArrivalLocationList);
                SourceArrivalLocationList = sourceArrivalLocationList;

                VisitedCountryCount = allArrivalLocationList.Select(x => x.CountryCode).Distinct().Count();
                VisitedRegionCount = allArrivalLocationList
                    .Where(x => !string.IsNullOrEmpty(x.AdminAreaName))
                    .Select(x => (x.CountryCode, x.AdminAreaName))
                    .Distinct()
                    .Count();
                CheckInCount = await _customLocationDataRepository.CountVisitedAsync();

                var distinctArrivalLocationList = sourceArrivalLocationList.GroupBy(x => x.CountryCode).Select(x => x.FirstOrDefault()).ToList();
                var arrivalCountryList = distinctArrivalLocationList.ConvertAll(x => new Country()
                {
                    CountryName = x?.CountryName,
                    CountryShortCode = x?.CountryCode,
                });

                var filteredCountryCode = FilteredCountry?.CountryShortCode ?? ALL_COUNTRY_CODE;
                var countryList = new List<Country>()
                {
                    new ()
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
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
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
#if DEBUG
                Console.WriteLine(ex.Message);
#endif

            }

            return [];
        }

        [RelayCommand]
        void SetRegion(string region)
        {
            SelectedRegion = region == nameof(AppResources.AdminArea) ? AppResources.AdminArea : AppResources.Country;
        }

        [RelayCommand]
        async Task ChooseCountryAsync()
        {
            var names = CountryList.Select(x => x.CountryName).ToArray();
            var choice = await Shell.Current.DisplayActionSheetAsync(AppResources.FilterByCountry, AppResources.Cancel, null, names);
            var country = CountryList.FirstOrDefault(x => x.CountryName == choice);
            if (country is not null)
            {
                FilteredCountry = country;
            }
        }

        // The two display options as a small menu; a tick marks the ones that are on.
        [RelayCommand]
        async Task ChooseDisplayOptionsAsync()
        {
            string Label(bool on, string text) => on ? $"✓ {text}" : text;
            var unique = Label(IsUnique, AppResources.NoDuplicateCountries);
            var hideDate = Label(DateIsNotVisible, AppResources.HideDateAndMonth);

            var choice = await Shell.Current.DisplayActionSheetAsync(AppResources.DisplayOptions, AppResources.Cancel, null, unique, hideDate);
            if (choice == unique)
            {
                IsUnique = !IsUnique;
            }
            else if (choice == hideDate)
            {
                DateIsNotVisible = !DateIsNotVisible;
            }
        }

        [RelayCommand]
        Task GoToMapAsync() => Shell.Current.GoToAsync("//Map");

        [RelayCommand]
        public async Task RefreshCountriesAsync()
        {
            IsRefreshing = true;
            await Init();
            IsRefreshing = false;
        }

        [RelayCommand]
        public void ChangeRegion()
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
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
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
            var roundedLongitude = Math.Round(longitude, 6);
            var latitude = arrivalLocation.Location.Latitude;
            var roundedLatitude = Math.Round(latitude, 6);

            var deletes = await Shell.Current.DisplayAlertAsync($"{AppResources.ArrivalLocationInfo}", $"\n{AppResources.Date}: {arrivalLocation.ArrivalDate}\n" +
                                                                    $"{AppResources.Country}: {arrivalLocation.CountryName}\n" +
                                                                    $"{AppResources.AdminArea}: {arrivalLocation.AdminAreaName}\n" +
                                                                    $"{AppResources.Locality}: {arrivalLocation.LocalityName}\n" +
                                                                    $"{AppResources.Location}: {roundedLatitude}, {roundedLongitude}", $"{AppResources.Delete}", "OK");
            if (!deletes) return;

            var confirmed = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.ConfirmDelete}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (confirmed)
            {
                await DeleteArrivalLocationAsync(arrivalLocation);
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
                if (_isMapInitialized) return;

                _isMapInitialized = true;
                // To Show the activity indicator before executing OnPropertyChanged
                await Task.Delay(100);

                OnPropertyChanged(nameof(ShapesSource));
                OnPropertyChanged(nameof(MapArrivalLocationList));
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
}