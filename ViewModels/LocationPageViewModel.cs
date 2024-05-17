using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;
using FlagsRally.Services;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private readonly IArrivalInfoService _arrivalInfoService;
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    public Microsoft.Maui.Controls.Maps.Map ArrivalMap;

    [ObservableProperty]
    ObservableCollection<ArrivalLocationPin> _positions;

    public LocationPageViewModel(IArrivalInfoService arrivalInfoService)
	{
        _arrivalInfoService = arrivalInfoService;
        _ = init();
    }

    private async Task init()
    {
        var arrivalLocationPins = await _arrivalInfoService.GetArrivalLocationPinsAsync();
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

            IEnumerable<Placemark> placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            Placemark placemark = placemarks?.FirstOrDefault();

            if (placemark == null)
                throw new Exception("Unable to get location");

            var result = await Shell.Current.DisplayAlert("Confirmation", $"Is the following your current location?\n\n" +
                                                            $"Country: {placemark?.CountryName}\n" +
                                                            $"Admin area: {placemark?.AdminArea}\n" +
                                                            $"Locality: {placemark?.Locality}", "Yes", "No");
            if (result)
            {
                var currentTime = DateTime.Now;
                var id = await _arrivalInfoService.Save(placemark, currentTime);
                ArrivalLocationPin arrivalLocationPins = new () 
                {
                    Id = id,
                    ArrivalDate = currentTime,
                    PinLocation = new Location
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    }
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
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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