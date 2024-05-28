using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        private readonly SettingsPreferences _settingsPreferences;
        private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
        private readonly CountryHelper _countryHelper;
        private const string ALL_COUNTRY_CODE = "All";
        private readonly string ALL_COUNTRY_NAME = AppResources.AllCountries;

        public MainPageViewModel(CountryHelper countryHelper, SettingsPreferences settingPreferences, IArrivalLocationDataRepository arrivalLocationRepository)
        {
            Title = "Main Page";

            _settingsPreferences = settingPreferences;
            _settingsPreferences.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PassportImageSourceString));
            _arrivalLocationRepository = arrivalLocationRepository;
            _countryHelper = countryHelper;

            _ = Init();
        }

        [ObservableProperty]
        bool _isSettingsVisible;

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

        public string PassportImageSourceString => $"https://www.passportindex.org/countries/{_settingsPreferences.GetCountryOrRegion().ToLower()}.png";

        [ObservableProperty]
        ObservableCollection<ArrivalLocation> _sourceArrivalLocationList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCountry), nameof(IsAdminArea))]
        string _selectedRegion = AppResources.Country;

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalLocationList))]
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
                var sourceArrivalLocationList = new ObservableCollection<ArrivalLocation>(await _arrivalLocationRepository.GetAllArivalLocations());
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
            await Shell.Current.DisplayAlert($"{AppResources.ArrivalLocationInfo}", $"\n{AppResources.Date}: {arrivalLocation.ArrivalDate}\n" +
                                                                    $"{AppResources.Country}: {arrivalLocation.CountryName}\n" +
                                                                    $"{AppResources.AdminArea}: {arrivalLocation.AdminAreaName}\n" +
                                                                    $"{AppResources.Locality}: {arrivalLocation.LocalityName}","OK");
        }
    }
}