using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        private CustomCountryHelper _countryHelper;
        private SettingsPreferences _settingPreferences;
        private CancellationTokenSource cancellationSource = new CancellationTokenSource();

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageSourceString))]
        private Country _selectedCountry;

        public string ImageSourceString => $"https://flagcdn.com/160x120/{SelectedCountry.CountryShortCode.ToLower()}.png";

        public SettingPageViewModel(SettingsPreferences settingPreferences, CustomCountryHelper countryHelper)
        {
            _settingPreferences = settingPreferences;

            _countryHelper = countryHelper;
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
                await Permissions.RequestAsync<Permissions.StorageRead>();
                await Permissions.RequestAsync<Permissions.StorageWrite>();
                var folderPickerResult = await FolderPicker.PickAsync(cancellationSource.Token);

                string downloadPath = Path.Combine(folderPickerResult.Folder.Path, Constants.DatabaseName);

                if (!Directory.Exists(Path.GetDirectoryName(downloadPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));
                }

                File.Copy(dbPath, downloadPath, true);
                await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}", "OK");
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task RestoreBackUpAsync()
        {
            try
            {
                var result = await Shell.Current.DisplayAlert($"{AppResources.AreYouSureRestoreBackup}", $"{AppResources.OverwriteExistingFile}\n" +
                    $"{AppResources.ActionCannotUndone}", $"{AppResources.Yes}", $"{AppResources.No}");

                if (!result) return;
                var pickedFile = await FilePicker.PickAsync();
                if (pickedFile?.FileName != Constants.DatabaseName)
                {
                    await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
                    return;
                }
                File.Copy(pickedFile.FullPath, Constants.DataBasePath, true);
                await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}\n" +
                $"{AppResources.RelaunchToEnable}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
            }
        }
    }
}