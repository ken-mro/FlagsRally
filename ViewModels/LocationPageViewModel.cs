using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Exceptions;
using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using Maui.GoogleMaps;
using Maui.RevenueCat.InAppBilling.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using Map = Maui.GoogleMaps.Map;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private const double DEFAULT_LATITUDE = 46.22667333333333;
    private const double DEFAULT_LONGITUDE = 6.140291666666666;
    private const double DEFAULT_ZOOM_LEVEL = 14d;
    private const double CLOSE_ZOOM_LEVEL = 18d;
    private const int MAP_UPDATE_DELAY_MS = 100;
    private const int PAUSE_DURATION_MS = 1000;
    private const double CLOSE_DISTANCE_THRESHOLD_KM = 0.05;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly ICustomBoardRepository _customBoardRepository;
    private readonly ICustomLocationDataRepository _customLocationDataRepository;
    private readonly CustomGeolocation _customGeolocation;
    private readonly AppShell _appShell;
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    private Map? _arrivalMap;
    private CustomBoardService _customBoardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectsCustomLocationPin))]
    Pin? _selectedPin;

    Pin? _tappedPointPin;

    public bool SelectsCustomLocationPin => (SelectedPin?.Tag as MapPinTag)?.IsCustomLocation ?? false;

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
            _arrivalMap.UiSettings.MapToolbarEnabled = true;
            _arrivalMap.InfoWindowLongClicked += async (sender, e) => await OnInfoWindowLongClicked(sender, e);
            _arrivalMap.MyLocationButtonClicked += async (sender, e) => await OnMyLocationButtonClickedAsync();
            _arrivalMap.MapClicked += (sender, e) => ClearTappedPointPin(sender, e);
            _arrivalMap.MapLongClicked += (sender, e) => ShowPinOnTappedPoint(sender, e);
            _arrivalMap.PinDragEnd += (sender, e) => _arrivalMap_PinDragEnd(sender, e);

            _ = Init();
        }
    }

    private void _arrivalMap_PinDragEnd(object? sender, PinDragEventArgs e)
    {
        var position = e.Pin.Position;

        var selectedLocationPin = e.Pin as SelectedLocationPin;
        selectedLocationPin?.UpdateLocation(position);

        if (ArrivalMap is not null)
        {
            ArrivalMap.SelectedPin = null;
            ArrivalMap.SelectedPin = e.Pin;
        }
    }

    private void ShowPinOnTappedPoint(object? sender, MapLongClickedEventArgs e)
    {
        if (_tappedPointPin is not null)
        {
            ArrivalMap?.Pins.Remove(_tappedPointPin);
            _tappedPointPin = null;
        }

        _tappedPointPin = new SelectedLocationPin(e.Point);

        ArrivalMap?.Pins.Add(_tappedPointPin);
        if (ArrivalMap is not null)
        {
            ArrivalMap.SelectedPin = _tappedPointPin;
        }
    }

    private void ClearTappedPointPin(object? sender, MapClickedEventArgs e)
    {
        if (_tappedPointPin is null) return;

        ArrivalMap?.Pins.Remove(_tappedPointPin);
        _tappedPointPin = null;
    }

    private async Task OnInfoWindowLongClicked(object? sender, InfoWindowLongClickedEventArgs e)
    {
        var pin = e.Pin;

        if (pin is ArrivalLocationPin arrivalLocationPin)
        {
            var deletes = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.ConfirmDelete}\n\n", $"{AppResources.Yes}", $"{AppResources.No}");
            if (!deletes) return;

            //update database
            var affectedRow = await _arrivalLocationRepository.DeleteAsync(arrivalLocationPin.Id);
            var deleteIsFailed = affectedRow != 1;

            if (deleteIsFailed)
            {
                    await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}\n\n", "OK");
                    return;
                }

            //update pin on map
                ArrivalMap?.Pins.Remove(pin);
            }
        else if (pin is CustomLocationPin customLocationPin)
        {
            if (!customLocationPin.IsVisited) return;

            var clears = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.ConfirmReset}\n\n", $"{AppResources.Yes}", $"{AppResources.No}");
            if (!clears) return;
            
            //update database
            var affectedRow = await _customLocationDataRepository.UpdateCustomLocation(customLocationPin.CustomLocationKey, null);
            var clearIsFailed = affectedRow != 1;

            if (clearIsFailed)
            {
                    await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}\n\n", "OK");
                    return;
                }

            //update pin on map
            customLocationPin.UpdateVisitStatus(null);

            if (ArrivalMap is null) return;
            ArrivalMap.SelectedPin = null;
            ArrivalMap.SelectedPin = customLocationPin;
        }        
    }

    private static double GetCloseDistanceThresholdKMFrom(double zoomLevel)
    {
        return -99.999 / 20 * (zoomLevel - 2) + 100;
    }

    private async Task OnMyLocationButtonClickedAsync()
    {
        var userLocation = await GetLastKnownOrDefaultLocationAsync();
        var currentCameraLocation = GetCurrentCameraLocation();
        var distance = userLocation.CalculateDistance(currentCameraLocation, DistanceUnits.Kilometers);

        var currentZoomLevel = ArrivalMap?.CameraPosition.Zoom ?? DEFAULT_ZOOM_LEVEL;
        var closeDistanceThresholdKM = GetCloseDistanceThresholdKMFrom(currentZoomLevel);
        var targetZoomLevel = distance > closeDistanceThresholdKM ? currentZoomLevel : Math.Max(currentZoomLevel, CLOSE_ZOOM_LEVEL);

        await Task.Delay(MAP_UPDATE_DELAY_MS); // Delay to allow map to update
        if (ArrivalMap is not null)
        {
            var position = new Position(userLocation.Latitude, userLocation.Longitude);
            await ArrivalMap.AnimateCamera(CameraUpdateFactory.NewPositionZoom(position, targetZoomLevel));
        }
    }

    private static async Task<Location> GetLastKnownOrDefaultLocationAsync()
    {
        return await Geolocation.Default.GetLastKnownLocationAsync() 
               ?? new Location(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
    }
    private async Task MoveAndZoomToCurrentLocationAsync()
    {
        var userLocation = await GetLastKnownOrDefaultLocationAsync();
        var currentCameraLocation = GetCurrentCameraLocation();
        var position = new Position(userLocation.Latitude, userLocation.Longitude);

        var currentZoom = ArrivalMap?.CameraPosition.Zoom ?? DEFAULT_ZOOM_LEVEL;
        var zoomLevel = Math.Max(currentZoom, CLOSE_ZOOM_LEVEL);
        await Task.Delay(MAP_UPDATE_DELAY_MS); // Delay to allow map to update
        if (ArrivalMap is not null)
        {
            await ArrivalMap.AnimateCamera(CameraUpdateFactory.NewPositionZoom(position, zoomLevel));
        }
    }

    private Location GetCurrentCameraLocation()
    {
        var cameraTarget = ArrivalMap?.CameraPosition.Target;
        return new Location(
            cameraTarget?.Latitude ?? DEFAULT_LATITUDE, 
            cameraTarget?.Longitude ?? DEFAULT_LONGITUDE
        );
    }

    private async Task<Location> GetCurrentLocation()
    {
        GeolocationRequest request = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
#if IOS
            request.RequestFullAccuracy = true;
#endif

        _cancelTokenSource = new CancellationTokenSource();
        var location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
        if (location is null)
            throw new Exception($"{AppResources.UnableToGetLocation}");

        if (location.IsFromMockProvider)
        {
            throw new FakeLocationException($"{AppResources.FakeLocationDetected}");
        }

        return location;
    }

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences, CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, AppShell appShell)
    {
        _appShell = appShell;
        _arrivalLocationRepository = arrivalLocationRepository;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
        _customBoardService = customBoardService;
    }

    private async Task Init()
    {
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            await InitializeMapPins();

            Location location = await GetLastKnownOrDefaultLocationAsync();
            var position = new Position(location.Latitude, location.Longitude);
            if (ArrivalMap is not null)
            {
                await ArrivalMap.MoveCamera(CameraUpdateFactory.NewPositionZoom(position, DEFAULT_ZOOM_LEVEL));
            }
        }
        catch (Exception ex)
        {
            // Unable to get location
#if DEBUG
            Console.WriteLine(ex.Message);
#endif
            var position = new Position(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
            if (ArrivalMap is not null)
            {
                await ArrivalMap.MoveCamera(CameraUpdateFactory.NewPositionZoom(position, DEFAULT_ZOOM_LEVEL));
            }
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

            if (SelectsCustomLocationPin)
            {
                await CheckInCustomLocation();
                return;
            }

            var arrivalLocationCount = (await _arrivalLocationRepository.GetAllArrivalLocations()).Count;
            if (arrivalLocationCount >= 5 && !_settingsPreferences.IsApiKeySet())
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            var currentLocation = await GetCurrentLocation();

            if (_tappedPointPin is null)
            {
                await MoveAndZoomToCurrentLocationAsync();

                var position = new Position(currentLocation.Latitude, currentLocation.Longitude);
                var currentPin = new SelectedLocationPin(position);

                _tappedPointPin = currentPin;
                ArrivalMap?.Pins.Add(_tappedPointPin);
                await Task.Delay(PAUSE_DURATION_MS);
            }
            else
            {
                var tappedPinLocation = new Location(_tappedPointPin.Position.Latitude, _tappedPointPin.Position.Longitude);

                var distance = tappedPinLocation.CalculateDistance(currentLocation, DistanceUnits.Kilometers);
                var isNear = distance <= CLOSE_DISTANCE_THRESHOLD_KM;
                if (!isNear)
                {
                    await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.YouAreNotNearTheLocation}", "OK");
                    return;
                }

                currentLocation = tappedPinLocation; 
            }

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(DateTime.Now, currentLocation, languageCode);

            if (arrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                await _arrivalLocationRepository.Save(arrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocationData.CountryCode);                
                ArrivalLocationPin arrivalLocationPin = new (arrivalLocationData);
                ArrivalMap?.Pins.Add(arrivalLocationPin);

                if (_tappedPointPin is null) return;

                ArrivalMap?.Pins.Remove(_tappedPointPin);
                _tappedPointPin = null;

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
        catch(FakeLocationException ex)
        {
            // Handle fake location exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}", "OK");
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

    private async Task CheckInCustomLocation()
    {
        var selectedCustomLocationPin = SelectedPin as CustomLocationPin;
        var pinPosition = selectedCustomLocationPin!.Position;
        var pinLocation = new Location(pinPosition.Latitude, pinPosition.Longitude);

        await MoveAndZoomToCurrentLocationAsync();
        var currentLocation = await GetCurrentLocation();

        var distance = pinLocation.CalculateDistance(currentLocation, DistanceUnits.Kilometers);
        var isNear = distance <= CLOSE_DISTANCE_THRESHOLD_KM;
        if (!isNear)
        {
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.YouAreNotNearTheLocation}", "OK");
            return;
        }

        var now = DateTime.Now;
        var affectedRow = await _customLocationDataRepository.UpdateCustomLocation(selectedCustomLocationPin.CustomLocationKey, now);
        
        if (affectedRow != 1)
        {
            throw new Exception($"{AppResources.FailedToCheckIn}");
        }

        selectedCustomLocationPin.UpdateVisitStatus(now);

        if (ArrivalMap is null) return;
        ArrivalMap.SelectedPin = null;
        ArrivalMap.SelectedPin = selectedCustomLocationPin;

        // Show the image popup for successful check-in
        await ShowCustomLocationImagePopup(selectedCustomLocationPin.CustomLocationKey, now);
    }

    private async Task ShowCustomLocationImagePopup(string customLocationKey, DateTime arrivalDate)
    {
        try
        {
            // Get the custom location data to create the popup
            var customLocation = await _customLocationDataRepository.GetCustomLocationByCompositeKey(customLocationKey);

            if (customLocation is not null)
            {
                // Update the arrival date to the current check-in time
                var updatedCustomLocation = new CustomLocation(
                    customLocation.Board,
                    customLocation.Code,
                    customLocation.Title,
                    customLocation.Subtitle,
                    customLocation.Group,
                    customLocation.Location,
                    arrivalDate
                );

                var popupViewModel = new CustomLocationImagePopupViewModel(updatedCustomLocation);
                var popup = new CustomLocationImagePopupView(popupViewModel);
                await Shell.Current.CurrentPage.ShowPopupAsync(popup);
            }
        }
        catch (Exception ex)
        {
            // If popup fails, don't block the check-in process
#if DEBUG
            Console.WriteLine($"Failed to show image popup: {ex.Message}");
#endif
        }
    }

    [RelayCommand]
    public async Task AddCustomBoardJsonAsync()
    {
        if (IsBusy || _isCheckingLocation)
            return;
        try
        {
            IsBusy = true;
            var pickedFile = await FilePicker.PickAsync();
            if (pickedFile is null) return;
            using var stream = await pickedFile.OpenReadAsync();
            (var customBoard, var pins) = await _customBoardService.SaveBoardAndLocations(stream, pickedFile.FileName);

            if (!_appShell.CustomBoardPage.IsVisible)
            {
                _appShell.CustomBoardPage.IsVisible = true;
            }

            var filterItem = new CustomBoardPinFilterItem(customBoard);
            if (!PinFilterList.Where(f => f.Name.Equals(filterItem.Name)).Any())
            {
                PinFilterList.Add(filterItem);
                FilteredPinItem = filterItem;
            }
            else
            {
                FilteredPinItem = PinFilterList.Where(f => f.Name.Equals(filterItem.Name)).FirstOrDefault() ?? FilteredPinItem;
            }

            AddPinsToMap(pins);
        }
        catch (OperationCanceledException)
        {
            // User cancelled password entry, just return without showing error
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CancelRequest()
    {
        if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
            _cancelTokenSource.Cancel();
    }

    [ObservableProperty]
    ObservableCollection<CustomBoardPinFilterItem> _pinFilterList = default!;

    CustomBoardPinFilterItem _filteredPinItem = default!;

    public CustomBoardPinFilterItem FilteredPinItem
    {
        get => _filteredPinItem;
        set
        {
            SetProperty(ref _filteredPinItem, value);
            UpdatePinsVisibility();
        }
    }

    private void UpdatePinsVisibility()
    {
        if (ArrivalMap?.Pins is null) return;
        foreach (var pin in ArrivalMap.Pins)
        {
            if (FilteredPinItem.IsAll)
            {
                pin.IsVisible = true;
                continue;
            }

            if (FilteredPinItem.Name ==(pin.Tag as MapPinTag)?.PinKey)
            {
                pin.IsVisible = true;
            }
            else
            {
                pin.IsVisible = false;
            }
        }
    }

    private async Task TryToOfferSubscription()
    {
        var customerInfo = await _revenueCat.GetCustomerInfo();
        var isSubscribed = customerInfo?.ActiveSubscriptions?.Count > 0;
        _settingsPreferences.SetIsSubscribed(isSubscribed);

        if (_settingsPreferences.GetIsSubscribed()) return;
        await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
    }

    private async Task InitializeMapPins()
    {
        PinFilterList = new(CustomBoardPinFilterItem.CreateFilterList());

        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        var arrivalLocationPin = arrivalLocationPins.FirstOrDefault();
        var tag = arrivalLocationPin?.Tag as MapPinTag;
        if (tag is not null)
        {
            PinFilterList.Add(new CustomBoardPinFilterItem(tag.PinKey));
        }

        AddPinsToMap(arrivalLocationPins);


        var boardList = await _customBoardRepository.GetAllCustomBoards();
        foreach (var board in boardList)
        {
            PinFilterList.Add(new CustomBoardPinFilterItem(board.Name));
        }

        var customLocationList = await _customLocationDataRepository.GetAllCustomLocationPins();
        AddPinsToMap(customLocationList);
    }

    private void AddPinsToMap(IEnumerable<Pin> customLocationList)
    {
        foreach (var pin in customLocationList)
        {
            ArrivalMap?.Pins.Add(pin);
        }
    }
}