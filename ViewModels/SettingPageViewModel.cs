using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using FlagsRally.Helpers;
using FlagsRally.Repository;
using FlagsRally.Resources;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

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

        [ObservableProperty]
        private string _apiKey;

        public string ImageSourceString => $"https://flagcdn.com/160x120/{SelectedCountry.CountryShortCode.ToLower()}.png";

        public SettingPageViewModel(SettingsPreferences settingPreferences, CustomCountryHelper countryHelper)
        {
            _settingPreferences = settingPreferences;
            ApiKey = _settingPreferences.GetApiKey();

            _countryHelper = countryHelper;
            CountryList = new ObservableCollection<Country>(_countryHelper.GetCountryData());

            _selectedCountry = _countryHelper.GetCountryByCode(_settingPreferences.GetCountryOfResidence());

            PropertyChanged += OnSelectedCountryChanged;
        }

        private void OnSelectedCountryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedCountry))
            {
                _settingPreferences.SetCountryOfResidence(SelectedCountry.CountryShortCode);
            }
        }

        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios15.0")]
        [SupportedOSPlatform("maccatalyst15.0")]
        [RelayCommand]
        async Task CreateBackUpAsync()
        {
            try
            {
                string dbPath = Constants.DataBasePath;
                await Permissions.RequestAsync<Permissions.StorageWrite>();
                var folderPickerResult = await FolderPicker.PickAsync(cancellationSource.Token);

                if (folderPickerResult.Folder is null) return;

                var folderPath = folderPickerResult.Folder.Path;
                string zipPath = Path.Combine(folderPath, Constants.BackupZipName);

                // Create password-protected zip file
                using (var zipStream = new ZipOutputStream(File.Create(zipPath)))
                {
                    zipStream.SetLevel(9); // Compression level (0-9)
                    zipStream.Password = Constants.DatabasePassword;

                    var entry = new ZipEntry(Constants.DatabaseName)
                    {
                        DateTime = DateTime.Now
                    };

                    zipStream.PutNextEntry(entry);

                    using (var fileStream = File.OpenRead(dbPath))
                    {
                        await fileStream.CopyToAsync(zipStream);
                    }

                    zipStream.CloseEntry();
                }

                await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}", "OK");
            }
            catch(UnauthorizedAccessException)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.AccessDenied}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task EnableApiKeyAsync()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                if (!string.IsNullOrEmpty(_settingPreferences.GetApiKey()))
                {
                    _settingPreferences.SetApiKey(string.Empty);
                    await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.ClearAPIKey}", "OK");
                }

                return;
            }

            var isApiKeyValid = await CustomGeolocation.ApiKeyIsValid(ApiKey);
            if (isApiKeyValid)
            {
                _settingPreferences.SetApiKey(ApiKey);
                await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.SetAPIKey}", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.InvalidAPIKey}", "OK");
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
                if (pickedFile is null) return;
                
                // Validate the backup file using the pattern from Constants
                if (!Constants.IsValidBackupFileName(pickedFile.FileName))
                {
                    await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
                    return;
                }

                // Extract database from password-protected zip file
                using (var zipStream = new ZipInputStream(File.OpenRead(pickedFile.FullPath)))
                {
                    zipStream.Password = Constants.DatabasePassword;

                    ZipEntry? entry;
                    bool databaseFound = false;

                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        if (entry.Name == Constants.DatabaseName)
                        {
                            databaseFound = true;
                            
                            using (var fileStream = File.Create(Constants.DataBasePath))
                            {
                                await zipStream.CopyToAsync(fileStream);
                            }
                            break;
                        }
                    }

                    if (!databaseFound)
                    {
                        await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
                        return;
                    }
                }

                await Shell.Current.DisplayAlert($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}\n" +
                $"{AppResources.RelaunchToEnable}", "OK");
            }
            catch (ZipException)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
            }
        }
    }
}