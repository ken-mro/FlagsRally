using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CountryData.Standard;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Helpers;
using FlagsRally.Messages;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using ICSharpCode.SharpZipLib.Zip;
using Maui.RevenueCat.InAppBilling.Services;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

namespace FlagsRally.ViewModels
{
    public partial class SettingPageViewModel : BaseViewModel
    {
        private CustomCountryHelper _countryHelper;
        private SettingsPreferences _settingPreferences;
        private IRevenueCatBilling _revenueCatBilling;
        private CancellationTokenSource cancellationSource = new CancellationTokenSource();

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageSourceString))]
        private Country _selectedCountry;

        [ObservableProperty]
        private string _apiKey;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SubscriptionStatusText))]
        private bool _isSubscribed;

        public string ImageSourceString => $"https://flagcdn.com/160x120/{SelectedCountry.CountryShortCode.ToLower()}.png";
        public string SubscriptionStatusText => IsSubscribed ? AppResources.Subscribed : AppResources.NotSubscribed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesClassicPins))]
        [NotifyPropertyChangedFor(nameof(UsesDropPins))]
        private PinStyle _pinStyle;

        public bool UsesClassicPins => PinStyle == PinStyle.Classic;
        public bool UsesDropPins => PinStyle == PinStyle.Drop;

        partial void OnPinStyleChanged(PinStyle value)
        {
            _settingPreferences.SetPinStyle(value);
            PinIcons.Style = value;
            WeakReferenceMessenger.Default.Send(new PinStyleChangedMessage());
        }

        [RelayCommand]
        void SetPinStyle(string style)
        {
            PinStyle = Enum.Parse<PinStyle>(style);
        }

        public SettingPageViewModel(SettingsPreferences settingPreferences, CustomCountryHelper countryHelper, IRevenueCatBilling revenueCatBilling)
        {
            _settingPreferences = settingPreferences;
            _revenueCatBilling = revenueCatBilling;
            ApiKey = _settingPreferences.GetApiKey();
            _pinStyle = _settingPreferences.GetPinStyle();
            _ = UpdateSubscriptionStatusAsync();

            _countryHelper = countryHelper;
            CountryList = new ObservableCollection<Country>(_countryHelper.GetCountryData());

            _selectedCountry = _countryHelper.GetCountryByCode(_settingPreferences.GetCountryOfResidence());

            PropertyChanged += OnSelectedCountryChanged;
        }

        private async Task UpdateSubscriptionStatusAsync()
        {
            try
            {
                var customerInfoResult = await _revenueCatBilling.GetCustomerInfo();
                var isSubscribed = customerInfoResult.IsSuccess && customerInfoResult.Value?.ActiveSubscriptions?.Count > 0;
                _settingPreferences.SetIsSubscribed(isSubscribed);
                IsSubscribed = isSubscribed;
            }
            catch (Exception)
            {
                // Handle error gracefully, possibly with default value
                IsSubscribed = _settingPreferences.GetIsSubscribed();
            }
        }

        private void OnSelectedCountryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedCountry))
            {
                _settingPreferences.SetCountryOfResidence(SelectedCountry.CountryShortCode);
            }
        }

        [RelayCommand]
        async Task ManageSubscriptionAsync()
        {
            try
            {
                // Refresh subscription status
                await UpdateSubscriptionStatusAsync();

                if (IsSubscribed)
                {
                    // Show thank you message if already subscribed
                    await Shell.Current.DisplayAlertAsync(
                        AppResources.Subscription.TrimEnd(':'), 
                        AppResources.ThankYouForSubscribing, 
                        "OK");
                }
                else
                {
                    // Show subscription paywall if not subscribed
                    await Shell.Current.CurrentPage.ShowPopupAsync(
                        new Views.PayWallView(new PayWallViewModel(_revenueCatBilling, _settingPreferences)));
                    
                    // Refresh subscription status after popup closes
                    IsSubscribed = _settingPreferences.GetIsSubscribed();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
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

                // Create password-protected zip file in memory
                using var memoryStream = new MemoryStream();
                using (var zipStream = new ZipOutputStream(memoryStream))
                {
                    zipStream.IsStreamOwner = false; // Prevent closing the underlying stream
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

                memoryStream.Position = 0;

                // Use FileSaver instead of FolderPicker for better iPadOS compatibility
                var fileSaverResult = await FileSaver.SaveAsync(Constants.BackupZipName, memoryStream, cancellationSource.Token);

                if (!fileSaverResult.IsSuccessful)
                {
                    if (fileSaverResult.Exception != null)
                    {
                        throw fileSaverResult.Exception;
                    }
                    return;
                }

                await Shell.Current.DisplayAlertAsync($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}", "OK");
            }
            catch (UnauthorizedAccessException)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.AccessDenied}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
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
                    await Shell.Current.DisplayAlertAsync($"{AppResources.Completed}", $"{AppResources.ClearAPIKey}", "OK");
                }

                return;
            }

            var isApiKeyValid = await CustomGeolocation.ApiKeyIsValid(ApiKey);
            if (isApiKeyValid)
            {
                _settingPreferences.SetApiKey(ApiKey);
                await Shell.Current.DisplayAlertAsync($"{AppResources.Completed}", $"{AppResources.SetAPIKey}", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.InvalidAPIKey}", "OK");
            }
        }

        [RelayCommand]
        async Task RestoreBackUpAsync()
        {
            try
            {
                var result = await Shell.Current.DisplayAlertAsync($"{AppResources.AreYouSureRestoreBackup}", $"{AppResources.OverwriteExistingFile}\n" +
                    $"{AppResources.ActionCannotUndone}", $"{AppResources.Yes}", $"{AppResources.No}");

                if (!result) return;

                var pickedFile = await FilePicker.PickAsync();
                if (pickedFile is null) return;
                
                // Validate the backup file using the pattern from Constants
                if (!Constants.IsValidBackupFileName(pickedFile.FileName))
                {
                    await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
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
                        await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
                        return;
                    }
                }

                await Shell.Current.DisplayAlertAsync($"{AppResources.Completed}", $"{AppResources.BackupSucceeded}\n" +
                $"{AppResources.RelaunchToEnable}", "OK");
            }
            catch (ZipException)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.InvalidFileSelected}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task OpenTermsOfUseAsync()
        {
            try
            {
                await Browser.Default.OpenAsync(Constants.TERMS_OF_USE_URL, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task OpenPrivacyPolicyAsync()
        {
            try
            {
                await Browser.Default.OpenAsync(Constants.PRIVACY_POLICY_URL, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
            }
        }
    }
}