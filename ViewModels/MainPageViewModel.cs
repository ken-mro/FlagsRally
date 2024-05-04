using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public MainPageViewModel(SettingsPreferences settingPreferences, IArrivalInfoService arrivalInfoService)
        {
            Title = "Main Page";
            _settingsPreferences = settingPreferences;
            _settingsPreferences.PropertyChanged += (s, e) => OnPropertyChanged(nameof(PassportImageSourceString));
            _arrivalInfoService = arrivalInfoService;

            _ = Init();

        }

        public string PassportImageSourceString => $"https://www.passportindex.org/countries/{_settingsPreferences.GetCountryOrRegion().ToLower()}.png";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayArrivalCountryList))]
        ObservableCollection<ArrivalCountry> _sourceArrivalCountryList;

        public ObservableCollection<ArrivalCountry> DisplayArrivalCountryList => GetFilteredList();

        [ObservableProperty]
        bool _isRefreshing = false;

        private async Task Init()
        {
            SourceArrivalCountryList = new ObservableCollection<ArrivalCountry>(await _arrivalInfoService.GetAllCountries());
        }

        private ObservableCollection<ArrivalCountry> GetFilteredList()
        {
            if (SourceArrivalCountryList is null)
                return new ObservableCollection<ArrivalCountry>();

            return new ObservableCollection<ArrivalCountry>(SourceArrivalCountryList.Reverse());
        }

        [RelayCommand]
        public async Task RefreshCountriesAsync()
        {
            IsRefreshing = true;
            await Init();
            IsRefreshing = false;
        }
    }
}