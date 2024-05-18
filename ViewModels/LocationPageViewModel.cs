using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;
using FlagsRally.Services;
using Microsoft.Maui.Controls.Maps;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private readonly IArrivalInfoService _arrivalInfoService;
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    private List<string> _countryCodeList;
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

        var arrivalLocationList = await _arrivalInfoService.GetAllCountries();
        _countryCodeList = arrivalLocationList.Select(x => x.CountryCode).Distinct().ToList();

        List<Geometry> geometryList = [];

        foreach (string countryCode in _countryCodeList)
        {
            var geometry = await CountryGeojson.GetMultiPolygonByCountryCode(countryCode);
            geometryList.Add(geometry);
        }

        foreach (var geometry in geometryList)
        {
            foreach (var coordinates in geometry.coordinates)
            {
                foreach (var coordinate in coordinates)
                {
                    var polygon = new Polygon
                    {
                        StrokeColor = Color.FromArgb("#1BA1E2"),
                        StrokeWidth = 8,
                        FillColor = Color.FromArgb("#881BA1E2"),
                    };

                    foreach (var position in coordinate)
                    {
                        polygon.Geopath.Add(new Location(position[1], position[0]));
                    }

                    ArrivalMap.MapElements.Add(polygon);
                }
            }
        }

        var jpSubRegionList = await _arrivalInfoService.GetSubRegionsByCountryCode("JP");
        var jpSubRegionCodeList = jpSubRegionList.Select(x => x.Code).Distinct().ToList();

        foreach (var subRegionCode in jpSubRegionCodeList)
        {
            var multiPolygon = await PrefecturesGeojson.GetSubRegionMultiPolygonBy(subRegionCode);
            foreach (var coordinates in multiPolygon.coordinates)
            {
                foreach (var coordinate in coordinates)
                {
                    var polygon = new Polygon
                    {
                        StrokeColor = Color.FromArgb("#FF8C00"),
                        StrokeWidth = 8,
                        FillColor = Color.FromArgb("#88FF8C00"),
                    };

                    foreach (var position in coordinate)
                    {
                        polygon.Geopath.Add(new Location(position[1], position[0]));
                    }

                    ArrivalMap.MapElements.Add(polygon);
                }
            }
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
                var currentTime = DateTime.Now;
                var id = await _arrivalInfoService.Save(placemark, currentTime);
                ArrivalLocationPin arrivalLocationPins = new()
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

                if (!_countryCodeList.Any(x => x == placemark.CountryCode))
                {
                    _countryCodeList.Add(placemark.CountryCode);

                    var multiPolygon = await CountryGeojson.GetMultiPolygonByCountryCode(placemark.CountryCode);

                    foreach (var coordinates in multiPolygon.coordinates)
                    {
                        foreach (var coordinate in coordinates)
                        {
                            var polygon = new Polygon
                            {
                                StrokeColor = Color.FromArgb("#FF0000"),
                                StrokeWidth = 8,
                                FillColor = Color.FromArgb("#88FF0000"),
                            };

                            foreach (var position in coordinate)
                            {
                                var point = new Location(position[1], position[0]);
                                polygon.Geopath.Add(point);
                            }
                            ArrivalMap.MapElements.Add(polygon);
                        }
                    };
                }
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