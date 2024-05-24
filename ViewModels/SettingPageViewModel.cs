using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Repository;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        private CountryHelper _countryHelper;
        private SettingsPreferences _settingPreferences;
        private CancellationTokenSource cancellationSource = new CancellationTokenSource();

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

        [RelayCommand]
        async Task CreateBackUpAsync()
        {
            try
            {
                string dbPath = Constants.DataBasePath;
                await Permissions.RequestAsync<Permissions.StorageWrite>();
                var folderPickerResult = await FolderPicker.PickAsync(cancellationSource.Token);

                string downloadPath = Path.Combine(folderPickerResult.Folder.Path, Constants.DatabaseName);

                if (!Directory.Exists(Path.GetDirectoryName(downloadPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));
                }

                File.Copy(dbPath, downloadPath, true);
                await Shell.Current.DisplayAlert("Completed", "Backup saved successfully!", "OK");
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task RestoreBackUpAsync()
        {
            try
            {
                await Shell.Current.DisplayAlert("Are you sure you want to restore the backup file?", $"This operation over write the existing file.\n" +
                    $"This action cannot be undone.", "Yes", "No");

                var pickedFile = await FilePicker.PickAsync();
                if (pickedFile?.FileName != Constants.DatabaseName)
                {
                    await Shell.Current.DisplayAlert("Error", "Invalid file selected", "OK");
                    return;
                }
                File.Copy(pickedFile.FullPath, Constants.DataBasePath, true);
                await Shell.Current.DisplayAlert("Completed", $"Backup restored successfully!\n" +
                $"Please relaunch the app to enable the data.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}