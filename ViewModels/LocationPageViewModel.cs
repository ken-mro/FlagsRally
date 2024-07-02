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
    public Microsoft.Maui.Controls.Maps.Map ArrivalMap;

    [ObservableProperty]
    ObservableCollection<ArrivalLocationPin> _positions;

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
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

            Location location = await Geolocation.Default.GetLastKnownLocationAsync();
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

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var cancelTokenSource = new CancellationTokenSource();
            Location location = await Geolocation.Default.GetLocationAsync(request, cancelTokenSource.Token);
            var datetime = DateTime.Now;

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

            if (arrivalLocationData == null)
                throw new Exception($"{AppResources.UnableToGetLocation}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                ArrivalLocationPin arrivalLocationPins = new()
                {
                    Id = id,
                    ArrivalDate = arrivalLocationData.ArrivalDate,
                    PinLocation = location
                };
                Positions.Add(arrivalLocationPins);
            }
        }
        // Catch one of the following exceptions:
        //   FeatureNotSupportedException
        //   FeatureNotEnabledException
        //   PermissionException
        catch (Exception ex)
        {
            // Unable to get location
            // Todo:add logger
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.UnableToGetLocation}\n{AppResources.PleaseTryAgain}", "OK");
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
}