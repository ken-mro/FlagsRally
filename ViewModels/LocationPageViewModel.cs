using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using FlagsRally.Views;
using Maui.RevenueCat.InAppBilling.Services;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly CustomGeolocation _customGeolocation;
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    public Microsoft.Maui.Controls.Maps.Map ArrivalMap;

    [ObservableProperty]
    ObservableCollection<ArrivalLocationPin> _positions;

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
        _ = init();
    }

    private async Task init()
    {
        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        Positions = new ObservableCollection<ArrivalLocationPin>(arrivalLocationPins);
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            Location location = await Geolocation.Default.GetLastKnownLocationAsync() 
                                ?? new Location(46.22667333333333, 6.140291666666666);
            MapSpan mapSpan = new MapSpan(location, 0.01, 0.01);
            ArrivalMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            // Unable to get location
            Location location = new Location(46.22667333333333, 6.140291666666666);
            MapSpan mapSpan = new MapSpan(location, 0.01, 0.01);
            ArrivalMap.MoveToRegion(mapSpan);
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

            if (Positions.Count >= 5)
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
#if IOS
            request.RequestFullAccuracy = true;
#endif

            _cancelTokenSource = new CancellationTokenSource();
            Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
            if (location is null)
                throw new Exception($"{AppResources.UnableToGetLocation}");

            var datetime = DateTime.Now;

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

            if (arrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocationData.CountryCode);
                ArrivalLocationPin arrivalLocationPins = new()
                {
                    Id = id,
                    ArrivalDate = arrivalLocationData.ArrivalDate,
                    PinLocation = location
                };
                Positions.Add(arrivalLocationPins);
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
        if (!_settingsPreferences.GetIsSubscribed())
        {
            await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
        }
    }
}