using CommunityToolkit.Mvvm.Input;
using FlagsRally.Services;
using Microsoft.Maui.Devices.Sensors;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private readonly IArrivalInfoService _arrivalInfoService;
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    public LocationPageViewModel(IArrivalInfoService arrivalInfoService)
	{
        _arrivalInfoService = arrivalInfoService;
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

#if DEBUG
            if (placemark != null)
            {
                System.Diagnostics.Debug.WriteLine($"CountryCode:{placemark.CountryCode},\nCountryName:{placemark.CountryName}\nAdminArea:{placemark.AdminArea},\nSubAdminArea:{placemark.SubAdminArea},\nthoroughfare:{placemark.Thoroughfare},\nLocality:{placemark.Locality}");
            }
#endif

            if (placemark == null)
                throw new Exception("Unable to get location");

            var result = await Shell.Current.DisplayAlert("Confirmation", $"Is the following your current location?\nSub admin area: {placemark?.SubAdminArea},\nAdmin area: {placemark?.AdminArea},\nCountry: {placemark?.CountryName}", "Yes", "No");
            if (result)
            {
                await _arrivalInfoService.Save(placemark);
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