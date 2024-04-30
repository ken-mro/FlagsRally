using CommunityToolkit.Mvvm.ComponentModel;
using CountryData.Standard;
using FlagsRally.Repository;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        private CountryHelper _countryHelper;
        private SettingsPreferences _settingPreferences;

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageSourceString))]
        private Country _selectedCountry;

        public string ImageSourceString => $"https://flagcdn.com/160x120/{SelectedCountry.CountryShortCode.ToLower()}.png";

        public SettingPageViewModel(SettingsPreferences settingPreferences)
        {
            _settingPreferences = settingPreferences;

            _countryHelper = new CountryHelper();
            CountryList = new ObservableCollection<Country>(_countryHelper.GetCountryData());

            _selectedCountry = _countryHelper.GetCountryByCode(_settingPreferences.GetCountryOrRegion());

            PropertyChanged += OnSelectedCountryChanged;
        }

        private void OnSelectedCountryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedCountry))
            {
                _settingPreferences.SetCountryOrRegion(SelectedCountry.CountryShortCode);
            }
        }
    }
}