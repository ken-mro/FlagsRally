using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    public LocationPageViewModel()
	{
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
            if (placemark != null)
            {
                System.Diagnostics.Debug.WriteLine($"\n Geocoded: {placemark.AdminArea}, {placemark.CountryName}, {placemark.CountryCode}, {placemark.Thoroughfare}, Locality: {placemark.Locality}");
            }

            if (placemark == null)
                throw new Exception("Unable to get location");

            await Shell.Current.DisplayAlert("Confirmation", $"Is the following your current location?\nLocality: {placemark?.Locality},\nAdmin area: {placemark?.AdminArea},\nCountry: {placemark?.CountryName}", "Yes", "No");
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