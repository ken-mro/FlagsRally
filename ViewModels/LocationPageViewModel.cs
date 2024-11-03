using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Views;
using Maui.GoogleMaps;
using Maui.RevenueCat.InAppBilling.Services;
using System.Globalization;
using Map = Maui.GoogleMaps.Map;
using MapSpan = Maui.GoogleMaps.MapSpan;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private const double DEFAULT_LATITUDE = 46.22667333333333;
    private const double DEFAULT_LONGITUDE = 6.140291666666666;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly CustomGeolocation _customGeolocation;
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    private Map? _arrivalMap;

    public Map? ArrivalMap
    {
        get => _arrivalMap;
        set
        {
            SetProperty(ref _arrivalMap, value);
            _arrivalMap!.MyLocationEnabled = true;
            _arrivalMap.UiSettings.MyLocationButtonEnabled = true;
            _arrivalMap.UiSettings.CompassEnabled = true;
            _arrivalMap.UiSettings.ScrollGesturesEnabled = true;
            _ = Init();
        }
    }

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
    }

    private async Task Init()
    {
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
            foreach(var pin in arrivalLocationPins)
            {
                ArrivalMap?.Pins.Add(pin);
            }

            Location location = await Geolocation.Default.GetLastKnownLocationAsync() 
                                ?? new Location(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
            var position = new Position(location.Latitude, location.Longitude);
            ArrivalMap?.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(5000)));
        }
        catch (Exception ex)
        {
            // Unable to get location
#if DEBUG
            Console.WriteLine(ex.Message);
#endif
            var position = new Position(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
            ArrivalMap?.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(5000)));
        }
        finally
        {
            IsBusy = false;
            _isCheckingLocation = false;
        }
    }

    [RelayCommand]
    public async Task GetCurrentLocationAsync()
    {
        if (IsBusy || _isCheckingLocation)
            return;
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            if (ArrivalMap?.Pins.Count >= 5 && !_settingsPreferences.IsApiKeySet())
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            GeolocationRequest request = new (GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
#if IOS
            request.RequestFullAccuracy = true;
#endif

            _cancelTokenSource = new CancellationTokenSource();
            var location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
            if (location is null)
                throw new Exception($"{AppResources.UnableToGetLocation}");

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(DateTime.Now, location, languageCode);

            if (arrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocationData.CountryCode);

                ArrivalLocationPin arrivalLocationPin = new(id, arrivalLocationData.ArrivalDate, location);
                ArrivalMap?.Pins.Add(arrivalLocationPin);
            }
        }
        catch (FeatureNotSupportedException ex)
        {
            // Handle not supported on device exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nNot supported on device.", "OK");
        }
        catch (FeatureNotEnabledException ex)
        {
            // Handle not enabled on device exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nNot enabled on device.", "OK");
        }
        catch (PermissionException ex)
        {
            // Handle permission exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nSomething wrong with the permission", "OK");
        }
        catch (Exception ex)
        {
            // Unable to get location
            // Todo:add logger
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
        finally
        {
            IsBusy = false;
            _isCheckingLocation = false;
        }
    }

    public void CancelRequest()
    {
        if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
            _cancelTokenSource.Cancel();
    }

    private async Task TryToOfferSubscription()
    {
        var customerInfo = await _revenueCat.GetCustomerInfo();
        var isSubscribed = customerInfo?.ActiveSubscriptions?.Count > 0;
        _settingsPreferences.SetIsSubscribed(isSubscribed);

        if (_settingsPreferences.GetIsSubscribed()) return;
        await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
    }
}