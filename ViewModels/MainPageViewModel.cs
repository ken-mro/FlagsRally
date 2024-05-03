using CommunityToolkit.Mvvm.ComponentModel;
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
        ObservableCollection<ArrivalCountry> _arrivalCountryList;

        //public async Task FireOnPropertyChanged()
        //{
        //    OnPropertyChanged(PassportImageSourceString);
        //}

        private async Task Init()
        {
            ArrivalCountryList = new ObservableCollection<ArrivalCountry>(await _arrivalInfoService.GetAllCountries());
        }
    }
}