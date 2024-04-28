using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models;
using FlagsRally.Repository;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        public CountryCode SelectedCountryCode => SelectedCountry.Code;

        public string ImageSourceString => $"https://www.passportindex.org/countries/{SelectedCountryCode.Value.ToLower()}.png";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCountryCode), nameof(ImageSourceString))]
        Country _selectedCountry = Country.GetDefault();

        public SettingPageViewModel(CountryRepository countryRepository)
        {
            CountryList = new ObservableCollection<Country>(countryRepository.GetCountries());
        }
    }
}