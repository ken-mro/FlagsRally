using CommunityToolkit.Mvvm.ComponentModel;
using CountryData.Standard;
using System.Collections.ObjectModel;
using System.Linq;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        private CountryHelper _countryHelper;

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCountryCode), nameof(ImageSourceString))]
        Country? _selectedCountry;

        public string SelectedCountryCode => SelectedCountry?.CountryShortCode ?? "jp";

        public string ImageSourceString => $"https://www.passportindex.org/countries/{SelectedCountryCode.ToLower()}.png";

        public SettingPageViewModel()
        {
            _countryHelper = new CountryHelper();
            CountryList = new ObservableCollection<Country>(_countryHelper.GetCountryData());
        }
    }
}