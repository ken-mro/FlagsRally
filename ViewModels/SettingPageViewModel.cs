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
using FlagsRally.Services;
using FlagsRally.Views;
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
        private ICustomBoardRepository _customBoardRepository;
        private CancellationTokenSource cancellationSource = new CancellationTokenSource();

        [ObservableProperty]
        ObservableCollection<Country> _countryList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImageSourceString))]
        private Country _selectedCountry;

        [ObservableProperty]
        private string _apiKey;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SubscriptionStatusText), nameof(CanChooseStampStyle), nameof(IsStampStyleLocked))]
        private bool _isSubscribed;

        // The stamp designs and tilt need a subscription or the user's own API key
        public bool CanChooseStampStyle => IsSubscribed || _settingPreferences.IsApiKeySet();
        public bool IsStampStyleLocked => !CanChooseStampStyle;

        public string ImageSourceString => FlagSources.FlagCdnUrl(SelectedCountry.CountryShortCode);
        public string SubscriptionStatusText => IsSubscribed ? AppResources.Subscribed : AppResources.NotSubscribed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesClassicPins))]
        [NotifyPropertyChangedFor(nameof(UsesDropPins))]
        [NotifyPropertyChangedFor(nameof(UsesCritterPins))]
        private PinStyle _pinStyle;

        public bool UsesClassicPins => PinStyle == PinStyle.Classic;
        public bool UsesDropPins => PinStyle == PinStyle.Drop;
        public bool UsesCritterPins => PinStyle == PinStyle.Critter;

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

        // Settings > Passport stamps: the design (a mix, one per country, or the same for every stamp,
        // chosen in a dropdown) and, apart from it, whether the stamps lean a little.
        public IReadOnlyList<StampDesignChoice> StampDesignChoices { get; } = StampDesignChoice.All();

        // The dropdown's list, open below its button
        [ObservableProperty]
        private bool _isStampDesignListOpen;

        [RelayCommand]
        async Task ToggleStampDesignList()
        {
            if (!await UnlockStampStyleAsync()) return;
            IsStampDesignListOpen = !IsStampDesignListOpen;
        }

        [RelayCommand]
        async Task PickStampDesign(StampDesignChoice choice)
        {
            if (!await UnlockStampStyleAsync()) return;
            SelectedStampDesign = choice;
            IsStampDesignListOpen = false;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStampMixed), nameof(IsStampByCountry), nameof(IsStampSame))]
        private StampStyle _stampStyle;

        public bool IsStampMixed => StampStyle.IsMixed;
        public bool IsStampByCountry => StampStyle.ByCountry;
        public bool IsStampSame => StampStyle.Pattern is not null;

        [ObservableProperty]
        private StampDesignChoice _selectedStampDesign;

        partial void OnSelectedStampDesignChanged(StampDesignChoice value)
        {
            if (!CanChooseStampStyle) return;
            foreach (var choice in StampDesignChoices) choice.IsSelected = choice == value;
            // Picking a design is choosing the same design for every stamp
            _settingPreferences.SetStampSamePattern(value.Pattern);
            SetStampStyle(StampStyle.Single(value.Pattern));
        }

        [ObservableProperty]
        private bool _stampTilt;

        partial void OnStampTiltChanged(bool value)
        {
            if (CanChooseStampStyle) _settingPreferences.SetStampTilt(value);
        }

        [ObservableProperty]
        private bool _showsEveryStamp;

        partial void OnShowsEveryStampChanged(bool value) => _settingPreferences.SetShowsEveryStamp(value);

        [RelayCommand]
        async Task SetStampMode(string mode)
        {
            if (!await UnlockStampStyleAsync()) return;
            SetStampStyle(mode switch
            {
                "Mixed" => StampStyle.Mixed,
                "ByCountry" => StampStyle.PerCountry,
                _ => StampStyle.Single(SelectedStampDesign.Pattern),
            });
        }

        // Locked: offers the subscription, as the subscribe button does. True once it is unlocked.
        async Task<bool> UnlockStampStyleAsync()
        {
            if (CanChooseStampStyle) return true;
            await ManageSubscriptionAsync();
            return CanChooseStampStyle;
        }

        void SetStampStyle(StampStyle style)
        {
            _settingPreferences.SetStampStyle(style);
            StampStyle = style;
        }

        // The pin previews leave out custom board pins while there are no custom boards.
        [ObservableProperty]
        private bool _hasCustomBoards;

        public async Task RefreshCustomBoardsAsync()
        {
            try
            {
                HasCustomBoards = (await _customBoardRepository.GetAllCustomBoards()).Any();
            }
            catch (Exception)
            {
                // Keep the previous state; the preview is only an illustration.
            }
        }

        public SettingPageViewModel(SettingsPreferences settingPreferences, CustomCountryHelper countryHelper, IRevenueCatBilling revenueCatBilling, ICustomBoardRepository customBoardRepository)
        {
            _settingPreferences = settingPreferences;
            _customBoardRepository = customBoardRepository;
            _revenueCatBilling = revenueCatBilling;
            ApiKey = _settingPreferences.GetApiKey();
            _pinStyle = _settingPreferences.GetPinStyle();
            _stampStyle = _settingPreferences.GetStampStyle();
            _stampTilt = _settingPreferences.GetStampTilt();
            _showsEveryStamp = _settingPreferences.GetShowsEveryStamp();
            var samePattern = _settingPreferences.GetStampSamePattern();
            _selectedStampDesign = StampDesignChoices.First(choice => choice.Pattern == samePattern);
            _selectedStampDesign.IsSelected = true;
            // The last known status, so the stamp settings open unlocked for a subscriber while it is checked again
            _isSubscribed = _settingPreferences.GetIsSubscribed();
            _ = UpdateSubscriptionStatusAsync();

            _countryHelper = countryHelper;
            CountryList = new ObservableCollection<Country>(_countryHelper.GetCountryData());

            // A code the country list does not know would leave the picker without a selection.
            _selectedCountry = _countryHelper.GetCountryByCode(_settingPreferences.GetCountryOfResidence())
                ?? _countryHelper.GetCountryByCode("US");

            PropertyChanged += OnSelectedCountryChanged;
        }

        // Without an answer from RevenueCat (offline, for one), the last known status stands
        private async Task UpdateSubscriptionStatusAsync() =>
            IsSubscribed = await SubscriptionStatus.RefreshAsync(_revenueCatBilling, _settingPreferences);

        private void OnSelectedCountryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedCountry))
            {
                _settingPreferences.SetCountryOfResidence(SelectedCountry.CountryShortCode);
            }
        }

        [RelayCommand]
        async Task ChooseCountryAsync()
        {
            var labels = CountryList.Select(country => $"{country.CountryFlag}\u00A0\u00A0{country.CountryName}").ToArray();
            var picked = await AppDialogs.PickOneAsync(AppResources.CountryOfResidence, labels, CountryList.IndexOf(SelectedCountry));
            if (picked is int index)
            {
                SelectedCountry = CountryList[index];
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
                    await ShowAlertAsync(AppResources.Subscription.TrimEnd(':'), AppResources.ThankYouForSubscribing);
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
                await ShowErrorAsync(ex.Message);
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
                    // Closing the save screen without saving is not an error.
                    if (fileSaverResult.Exception is not null and not OperationCanceledException)
                    {
                        throw fileSaverResult.Exception;
                    }
                    return;
                }

                await ShowAlertAsync(AppResources.Completed, AppResources.BackupSucceeded);
            }
            catch (OperationCanceledException)
            {
                // Closed without saving
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorAsync(AppResources.AccessDenied);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
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
                    OnPropertyChanged(nameof(CanChooseStampStyle));
                    OnPropertyChanged(nameof(IsStampStyleLocked));
                    await ShowAlertAsync(AppResources.Completed, AppResources.ClearAPIKey);
                }

                return;
            }

            var isApiKeyValid = await CustomGeolocation.ApiKeyIsValid(ApiKey);
            if (isApiKeyValid)
            {
                _settingPreferences.SetApiKey(ApiKey);
                OnPropertyChanged(nameof(CanChooseStampStyle));
                OnPropertyChanged(nameof(IsStampStyleLocked));
                await ShowAlertAsync(AppResources.Completed, AppResources.SetAPIKey);
            }
            else
            {
                await ShowErrorAsync(AppResources.InvalidAPIKey);
            }
        }

        [RelayCommand]
        async Task RestoreBackUpAsync()
        {
            try
            {
                var result = await ConfirmAsync(AppResources.AreYouSureRestoreBackup, $"{AppResources.OverwriteExistingFile}\n" +
                    $"{AppResources.ActionCannotUndone}");

                if (!result) return;

                var pickedFile = await FilePicker.PickAsync();
                if (pickedFile is null) return;
                
                // Validate the backup file using the pattern from Constants
                if (!Constants.IsValidBackupFileName(pickedFile.FileName))
                {
                    await ShowErrorAsync(AppResources.InvalidFileSelected);
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
                        await ShowErrorAsync(AppResources.InvalidFileSelected);
                        return;
                    }
                }

                await ShowAlertAsync(AppResources.Completed, $"{AppResources.RestoreSucceeded}\n" +
                $"{AppResources.RelaunchToEnable}");
            }
            catch (ZipException)
            {
                await ShowErrorAsync(AppResources.InvalidFileSelected);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }
        }

        [RelayCommand]
        async Task OpenTermsOfUseAsync()
        {
            try
            {
                await Browser.Default.OpenAsync(LegalLinks.TermsOfUse(), BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }
        }

        // The how-to pages from the first launch, to read again.
        [RelayCommand]
        Task ShowHowToUseAsync() => OnboardingPopupView.ShowAsync(_settingPreferences);

        [RelayCommand]
        async Task OpenPrivacyPolicyAsync()
        {
            try
            {
                await Browser.Default.OpenAsync(LegalLinks.PrivacyPolicy(), BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex.Message);
            }
        }
    }
}