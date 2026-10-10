using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Exceptions;
using FlagsRally.Helpers;
using FlagsRally.Messages;
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
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly ICustomBoardRepository _customBoardRepository;
    private readonly ICustomLocationDataRepository _customLocationDataRepository;
    private readonly CustomGeolocation _customGeolocation;
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    private Map? _arrivalMap;
    private CustomBoardService _customBoardService;
    private ArrivalLocationService _arrivalLocationService;
    private readonly MapFocusRequest _mapFocusRequest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectsCustomLocationPin))]
    [NotifyPropertyChangedFor(nameof(HasPinDetails))]
    [NotifyPropertyChangedFor(nameof(HasNoPinDetails))]
    [NotifyPropertyChangedFor(nameof(SelectedPinTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedPinSubtitle))]
    [NotifyPropertyChangedFor(nameof(CanCheckIn))]
    [NotifyPropertyChangedFor(nameof(CanRemoveSelectedPin))]
    [NotifyPropertyChangedFor(nameof(RemoveSelectedPinText))]
    [NotifyPropertyChangedFor(nameof(ShowsCheckInSpotGuide))]
    [NotifyPropertyChangedFor(nameof(ShowsGetLocationButton))]
    [NotifyPropertyChangedFor(nameof(ShowsMapActions))]
    Pin? _selectedPin;

    // The check-in spot: dropped with a long press (or at your location by Get Location) and
    // dragged to where you want to record, as long as it stays within CheckInRange of you.
    Pin? _tappedPointPin;
    bool _isDraggingPin;

    // Where you were when the spot was placed, and the circle showing the range around it.
    Location? _checkInOrigin;
    Circle? _checkInRangeCircle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsCheckInSpotGuide))]
    [NotifyPropertyChangedFor(nameof(ShowsGetLocationButton))]
    [NotifyPropertyChangedFor(nameof(ShowsMapActions))]
    bool _hasCheckInSpot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckInSpotIsInRange))]
    [NotifyPropertyChangedFor(nameof(CheckInSpotIsOutOfRange))]
    [NotifyPropertyChangedFor(nameof(CheckInSpotDistanceText))]
    double? _checkInSpotDistanceKm;

    public bool ShowsCheckInSpotGuide => HasCheckInSpot && HasNoPinDetails;
    public bool ShowsGetLocationButton => !HasCheckInSpot && HasNoPinDetails;

    // Directions and open-in-Maps float over the map, like Google's own toolbar, while a card shows a place.
    public bool ShowsMapActions => HasPinDetails || ShowsCheckInSpotGuide;

    public string CheckInSpotHint => string.Format(AppResources.CheckInSpotHint, CheckInRange.LimitText);
    public bool CheckInSpotIsInRange => CheckInSpotDistanceKm is double d && CheckInRange.IsWithin(d);
    public bool CheckInSpotIsOutOfRange => CheckInSpotDistanceKm is double d && !CheckInRange.IsWithin(d);
    public string CheckInSpotDistanceText => CheckInSpotDistanceKm switch
    {
        double d when CheckInRange.IsWithin(d) => string.Format(AppResources.CheckInSpotInRange, CheckInRange.FormatDistance(d)),
        double d => string.Format(AppResources.CheckInSpotOutOfRange, CheckInRange.FormatDistance(d)),
        _ => string.Empty,
    };
    public string CheckInSpotIcon => $"{PinIcons.FileName(PinKind.CheckInSpot, PinIcons.Style)}.png";

    [RelayCommand]
    void CancelCheckInSpot() => RemoveCheckInSpot();

    private void PlaceCheckInSpot(Position position, Location? here)
    {
        RemoveCheckInSpot();
        _tappedPointPin = new SelectedLocationPin(position);
        ArrivalMap?.Pins.Add(_tappedPointPin);
        HasCheckInSpot = true;
        _ = ShowCheckInRangeAsync(here);
    }

    private void RemoveCheckInSpot()
    {
        if (_tappedPointPin is not null)
        {
            RemovePinFromMap(_tappedPointPin);
            _tappedPointPin = null;
        }
        if (_checkInRangeCircle is not null)
        {
            ArrivalMap?.Circles.Remove(_checkInRangeCircle);
            _checkInRangeCircle = null;
        }
        _checkInOrigin = null;
        HasCheckInSpot = false;
        CheckInSpotDistanceKm = null;
    }

    // Circles the area around you where the spot can be put. Without a known location there is
    // no circle yet; Get Location then fixes the location and draws it.
    private async Task ShowCheckInRangeAsync(Location? here)
    {
        try
        {
            here ??= await Geolocation.Default.GetLastKnownLocationAsync();
        }
        catch (Exception)
        {
            // The hint still explains the range; the circle appears once Get Location finds you.
        }
        if (here is null || _tappedPointPin is null || ArrivalMap is null) return;

        _checkInOrigin = here;
        if (_checkInRangeCircle is not null)
        {
            ArrivalMap.Circles.Remove(_checkInRangeCircle);
        }
        _checkInRangeCircle = new Circle
        {
            Center = new Position(here.Latitude, here.Longitude),
            Radius = Distance.FromKilometers(CheckInRange.LimitKm),
            StrokeColor = Color.FromArgb("#182A52"),
            StrokeWidth = 2f,
            FillColor = Color.FromArgb("#33182A52"),
        };
        ArrivalMap.Circles.Add(_checkInRangeCircle);
        UpdateCheckInSpotDistance();
    }

    private static Task ShowCheckInSpotTooFarAsync(double distanceKm)
    {
        var message = string.Format(AppResources.CheckInSpotTooFar, CheckInRange.FormatDistance(distanceKm), CheckInRange.LimitText);
        return Shell.Current.DisplayAlertAsync($"{AppResources.Error}", message, "OK");
    }

    private void UpdateCheckInSpotDistance()
    {
        if (_tappedPointPin is null || _checkInOrigin is null) return;

        var spot = new Location(_tappedPointPin.Position.Latitude, _tappedPointPin.Position.Longitude);
        CheckInSpotDistanceKm = spot.CalculateDistance(_checkInOrigin, DistanceUnits.Kilometers);
    }

    public bool SelectsCustomLocationPin => (SelectedPin?.Tag as MapPinTag)?.IsCustomLocation ?? false;

    // The details panel is shown for saved pins (arrival locations and custom board locations),
    // not for the temporary pin dropped by a long press.
    public bool HasPinDetails => SelectedPin is CustomLocationPin or ArrivalLocationPin;
    public bool HasNoPinDetails => !HasPinDetails;

    public string SelectedPinTitle => SelectedPin?.Label ?? string.Empty;

    public string SelectedPinSubtitle => SelectedPin switch
    {
        CustomLocationPin pin => $"{((MapPinTag)pin.Tag).BoardName} · {(pin.IsVisited ? pin.Address : AppResources.NotVisited)}",
        ArrivalLocationPin pin => $"{AppResources.ArrivalLocation} · {pin.Address}",
        _ => string.Empty,
    };

    public bool CanCheckIn => SelectedPin is CustomLocationPin;

    public bool CanRemoveSelectedPin => SelectedPin is ArrivalLocationPin or CustomLocationPin { IsVisited: true };

    public string RemoveSelectedPinText => SelectedPin is ArrivalLocationPin ? AppResources.Delete : AppResources.ResetCheckIn;

    [RelayCommand]
    async Task RemoveSelectedPinAsync()
    {
        if (SelectedPin is not null)
        {
            await DeleteOrResetPinAsync(SelectedPin);
        }
    }

    // Driving directions to the selected pin (or the check-in spot) in the device's maps app.
    [RelayCommand]
    Task OpenDirectionsAsync() => OpenInMapsAppAsync(NavigationMode.Driving);

    // The selected pin (or the check-in spot) shown in the device's maps app.
    [RelayCommand]
    Task OpenInMapsAsync() => OpenInMapsAppAsync(NavigationMode.None);

    private async Task OpenInMapsAppAsync(NavigationMode mode)
    {
        var pin = SelectedPin ?? _tappedPointPin;
        if (pin is null) return;

        try
        {
            var options = new MapLaunchOptions { Name = pin.Label, NavigationMode = mode };
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(pin.Position.Latitude, pin.Position.Longitude, options);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
        }
    }

    [RelayCommand]
    void ClosePinDetails()
    {
        if (ArrivalMap is not null)
        {
            ArrivalMap.SelectedPin = null;
        }
    }

    // Pin equality is value-based (label/position), so remove the exact instance.
    private void RemovePinFromMap(Pin pin)
    {
        if (ArrivalMap is null) return;

        for (int i = ArrivalMap.Pins.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(ArrivalMap.Pins[i], pin))
            {
                ArrivalMap.Pins.RemoveAt(i);
                return;
            }
        }
    }

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
            // Google's directions / open-in-Maps buttons would also be hidden under the panels; the
            // pin details and check-in spot cards carry the same two actions instead.
            _arrivalMap.UiSettings.MapToolbarEnabled = false;
            // The built-in zoom buttons would be hidden by the panels that float over the map; the page
            // has its own, which move up above whichever panel is showing.
            _arrivalMap.UiSettings.ZoomControlsEnabled = false;
            _arrivalMap.InfoWindowLongClicked += async (sender, e) => await DeleteOrResetPinAsync(e.Pin);
            _arrivalMap.MyLocationButtonClicked += async (sender, e) => await OnMyLocationButtonClickedAsync();
            _arrivalMap.MapClicked += (sender, e) => ClearTappedPointPin(sender, e);
            _arrivalMap.MapLongClicked += (sender, e) => ShowPinOnTappedPoint(sender, e);
            _arrivalMap.PinDragStart += (sender, e) => _isDraggingPin = true;
            _arrivalMap.PinDragEnd += (sender, e) => _arrivalMap_PinDragEnd(sender, e);
            _arrivalMap.PinClicked += OnPinClicked;

            _ = Init();
        }
    }

    private void _arrivalMap_PinDragEnd(object? sender, PinDragEventArgs e)
    {
        _isDraggingPin = false;
        var position = e.Pin.Position;

        var selectedLocationPin = e.Pin as SelectedLocationPin;
        selectedLocationPin?.UpdateLocation(position);
        UpdateCheckInSpotDistance();

        if (ArrivalMap is not null)
        {
            ArrivalMap.SelectedPin = null;
            ArrivalMap.SelectedPin = e.Pin;
        }
    }

    // Only the top marker of pins at the same coordinates can be tapped, so let the user choose among them.
    private void OnPinClicked(object? sender, PinClickedEventArgs e)
    {
        if (ArrivalMap is null) return;

        var overlappingPins = PinOverlapHelper.FindOverlapping(e.Pin, ArrivalMap.Pins);
        if (overlappingPins.Count < 2) return;

        e.Handled = true;
        MainThread.BeginInvokeOnMainThread(async () => await ChooseOverlappingPinAsync(overlappingPins));
    }

    private async Task ChooseOverlappingPinAsync(IReadOnlyList<Pin> overlappingPins)
    {
        var labels = PinOverlapHelper.GetChoiceLabels(overlappingPins);
        var choice = await Shell.Current.DisplayActionSheetAsync($"{AppResources.ChooseLocation}", $"{AppResources.Cancel}", null, [.. labels]);

        var index = labels.ToList().IndexOf(choice);
        if (index < 0 || ArrivalMap is null) return;

        ArrivalMap.SelectedPin = overlappingPins[index];
    }

    private void ShowPinOnTappedPoint(object? sender, MapLongClickedEventArgs e)
    {
        // On iOS the long press that starts dragging a pin is also reported as a map long press.
        // Replacing the spot then would take the pin being dragged off the map.
        if (_isDraggingPin) return;

        PlaceCheckInSpot(e.Point, here: null);
        if (ArrivalMap is not null)
        {
            ArrivalMap.SelectedPin = _tappedPointPin;
        }
    }

    private void ClearTappedPointPin(object? sender, MapClickedEventArgs e)
    {
        if (_tappedPointPin is null) return;

        RemoveCheckInSpot();
    }

    private async Task DeleteOrResetPinAsync(Pin pin)
    {

        if (pin is ArrivalLocationPin arrivalLocationPin)
        {
            var deletes = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.ConfirmDelete}\n\n", $"{AppResources.Yes}", $"{AppResources.No}");
            if (!deletes) return;

            //update database
            var affectedRow = await _arrivalLocationRepository.DeleteAsync(arrivalLocationPin.Id);
            var deleteIsFailed = affectedRow != 1;

            if (deleteIsFailed)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}\n\n", "OK");
                return;
            }

            //update pin on map
            if (ArrivalMap is not null && ReferenceEquals(ArrivalMap.SelectedPin, pin))
            {
                ArrivalMap.SelectedPin = null;
            }
            RemovePinFromMap(pin);
        }
        else if (pin is CustomLocationPin customLocationPin)
        {
            if (!customLocationPin.IsVisited) return;

            var clears = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.ConfirmReset}\n\n", $"{AppResources.Yes}", $"{AppResources.No}");
            if (!clears) return;

            //update database
            var affectedRow = await _customLocationDataRepository.UpdateCustomLocation(customLocationPin.CustomLocationKey, null);
            var clearIsFailed = affectedRow != 1;

            if (clearIsFailed)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}\n\n", "OK");
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
            await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(position, targetZoomLevel), animate: true);
        }
    }

    private static readonly TimeSpan CameraMoveTimeout = TimeSpan.FromSeconds(3);

    // The map's camera tasks sometimes never complete (e.g. while the map is still being laid out),
    // which would leave IsBusy set and block Get Location. Wait a short time at most.
    [RelayCommand]
    Task ZoomIn() => ZoomByAsync(1);

    [RelayCommand]
    Task ZoomOut() => ZoomByAsync(-1);

    private Task ZoomByAsync(double step)
    {
        if (ArrivalMap is null) return Task.CompletedTask;
        var camera = ArrivalMap.CameraPosition;
        return MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(camera.Target, Math.Clamp(camera.Zoom + step, 2, 21)), animate: true);
    }

    private async Task MoveCameraSafelyAsync(CameraUpdate update, bool animate)
    {
        if (ArrivalMap is null) return;
        try
        {
            var move = animate ? ArrivalMap.AnimateCamera(update) : ArrivalMap.MoveCamera(update);
            await move.WaitAsync(CameraMoveTimeout);
        }
        catch (TimeoutException)
        {
#if DEBUG
            Console.WriteLine("Camera move did not complete in time; continuing.");
#endif
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
            await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(position, zoomLevel), animate: true);
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

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences, CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, ArrivalLocationService arrivalLocationService, MapFocusRequest mapFocusRequest)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
        _customBoardService = customBoardService;
        _arrivalLocationService = arrivalLocationService;
        _mapFocusRequest = mapFocusRequest;
        PinIcons.Style = settingsPreferences.GetPinStyle();

        WeakReferenceMessenger.Default.Register<CustomBoardsChangedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(async () => await ((LocationPageViewModel)recipient).OnCustomBoardsChanged(message)));
        WeakReferenceMessenger.Default.Register<PinStyleChangedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(((LocationPageViewModel)recipient).RedrawPins));
    }

    // Gives every pin already on the map the icon of the newly chosen style.
    private void RedrawPins()
    {
        if (ArrivalMap is null) return;

        foreach (var pin in ArrivalMap.Pins)
        {
            if (PinIcons.ForPin(pin) is BitmapDescriptor icon)
            {
                pin.Icon = icon;
            }
        }
        OnPropertyChanged(nameof(CheckInSpotIcon));
    }

    bool _pinsLoaded;

    /// <summary>
    /// Selects the location another page asked to show (see <see cref="MapFocusRequest"/>).
    /// Called when the page appears and again once the pins have loaded.
    /// </summary>
    public async Task ShowRequestedLocationAsync()
    {
        if (!_pinsLoaded || ArrivalMap is null) return;

        var key = _mapFocusRequest.Take();
        if (key is not null)
        {
            await ShowCustomLocationAsync(key);
        }
    }

    private async Task ShowCustomLocationAsync(string compositeKey)
    {
        if (ArrivalMap is null) return;

        var pin = ArrivalMap.Pins.OfType<CustomLocationPin>().FirstOrDefault(p => p.CustomLocationKey == compositeKey);
        if (pin is null) return;

        // Make sure a filter does not hide the requested pin.
        if (!pin.IsVisible)
        {
            _visiblePinKeys.Clear();
            OnPinFilterChanged();
        }

        await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(pin.Position, DEFAULT_ZOOM_LEVEL), animate: true);
        ArrivalMap.SelectedPin = pin;
    }

    private async Task OnCustomBoardsChanged(CustomBoardsChangedMessage message)
    {
        try
        {
            var knownBoards = FilterChips.Select(c => c.Name).ToHashSet();
            if (message.PlacesChanged)
            {
                await ReloadCustomLocationPinsAsync();
            }
            await RebuildPinFilterList();

            // When filtering, also show boards that were just added.
            if (_visiblePinKeys.Count > 0)
            {
                _visiblePinKeys.UnionWith(FilterChips.Where(c => !c.IsAll && !knownBoards.Contains(c.Name)).Select(c => c.Name));
                OnPinFilterChanged();
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Failed to refresh pin filters: {ex.Message}");
#endif
        }
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
                await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(position, DEFAULT_ZOOM_LEVEL), animate: false);
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
                await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(position, DEFAULT_ZOOM_LEVEL), animate: false);
            }
        }
        finally
        {
            IsBusy = false;
            _isCheckingLocation = false;
        }

        _pinsLoaded = true;
        await ShowRequestedLocationAsync();
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

            // Get Location does what a long press does, at where you are: it puts the check-in spot
            // there and shows its card. Recording is then always "Check in here" on that card.
            if (_tappedPointPin is null)
            {
                var here = await GetCurrentLocation();
                await MoveAndZoomToCurrentLocationAsync();
                PlaceCheckInSpot(new Position(here.Latitude, here.Longitude), here);
                if (ArrivalMap is not null)
                {
                    ArrivalMap.SelectedPin = _tappedPointPin;
                }
                return;
            }

            // A spot already shown outside the circle cannot be recorded; say so before anything else.
            if (CheckInSpotDistanceKm is double spotDistance && !CheckInRange.IsWithin(spotDistance))
            {
                await ShowCheckInSpotTooFarAsync(spotDistance);
                return;
            }

            var arrivalLocationCount = (await _arrivalLocationRepository.GetAllArrivalLocations()).Count;
            if (arrivalLocationCount >= 5 && !_settingsPreferences.IsApiKeySet())
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            var currentLocation = await GetCurrentLocation();
            var tappedPinLocation = new Location(_tappedPointPin.Position.Latitude, _tappedPointPin.Position.Longitude);
            await ShowCheckInRangeAsync(currentLocation);

            var distance = tappedPinLocation.CalculateDistance(currentLocation, DistanceUnits.Kilometers);
            if (!CheckInRange.IsWithin(distance))
            {
                await ShowCheckInSpotTooFarAsync(distance);
                return;
            }

            currentLocation = tappedPinLocation;

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var rawArrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(DateTime.Now, currentLocation, languageCode);

            if (rawArrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{rawArrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                // Check if this is the first time visiting this country before saving
                var isFirstTimeCountry = !await _arrivalLocationService.HasVisitedCountryBefore(rawArrivalLocationData.CountryCode);

                SubRegionCode? subRegionCode = null;
                bool isFirstTimeSupportedSubRegion = false;
                if (Constants.SupportedSubRegionCountryCodeList.Any(c => c.Equals(rawArrivalLocationData.CountryCode.ToLower())))
                {
                    subRegionCode = new SubRegionCode(rawArrivalLocationData.CountryCode, rawArrivalLocationData.AdminAreaCode);
                    isFirstTimeSupportedSubRegion = !await _arrivalLocationService.HasVisitedSubRegionBefore(subRegionCode);
                }

                var arrivalLocation = await _arrivalLocationRepository.Save(rawArrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocation.CountryCode);
                ArrivalLocationPin arrivalLocationPin = new(arrivalLocation);
                ArrivalMap?.Pins.Add(arrivalLocationPin);

                if (isFirstTimeCountry)
                {
                    await ShowLocationDiscoveryPopup(arrivalLocation);
                }

                if (isFirstTimeSupportedSubRegion && subRegionCode is not null)
                {
                    await ShowLocationDiscoveryPopup(arrivalLocation.AdminAreaName, subRegionCode);
                }

                RemoveCheckInSpot();

            }
        }
        catch (FeatureNotSupportedException ex)
        {
            // Handle not supported on device exception
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\nNot supported on device.", "OK");
        }
        catch (FeatureNotEnabledException ex)
        {
            // Handle not enabled on device exception
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\nNot enabled on device.", "OK");
        }
        catch (PermissionException ex)
        {
            // Handle permission exception
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\nSomething wrong with the permission", "OK");
        }
        catch(FakeLocationException ex)
        {
            // Handle fake location exception
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}", "OK");
        }
        catch (Exception ex)
        {
            // Unable to get location
            // Todo:add logger
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
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
        if (!CheckInRange.IsWithin(distance))
        {
            // Say how far away the location is and offer directions to it.
            var message = $"{AppResources.YouAreNotNearTheLocation}\n{string.Format(AppResources.DistanceFromHere, DistanceFormatter.Format(distance))}";
            var wantsDirections = await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", message, $"{AppResources.Directions}", "OK");
            if (wantsDirections)
            {
                await OpenDirectionsAsync();
            }
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
            if (customLocation is null) return;

            var popupViewModel = new CustomLocationImagePopupViewModel(customLocation);
            var popup = new CustomLocationImagePopupView(popupViewModel);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);

        }
        catch (Exception ex)
        {
            // If popup fails, don't block the check-in process
#if DEBUG
            Console.WriteLine($"Failed to show image popup: {ex.Message}");
#endif
        }
    }

    private static async Task ShowLocationDiscoveryPopup(ArrivalLocation arrivalLocation)
    {
        try
        {
            string imageSource = arrivalLocation.CountryFlagSource;
            string title = string.Empty;

            title = $"{AppResources.NewCountry}: {arrivalLocation.CountryName}";

            // Only show popup if we have an image to display
            if (string.IsNullOrEmpty(imageSource)) return;

            var popupViewModel = new LocationDiscoveryPopupViewModel(imageSource, true, title);
            var popup = new LocationDiscoveryPopupView(popupViewModel);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            // If popup fails, don't block the location saving process
#if DEBUG
            Console.WriteLine($"Failed to show location discovery popup: {ex.Message}");
#endif
        }
    }

    private static async Task ShowLocationDiscoveryPopup(string subRegionTitle, SubRegionCode subRegionCode)
    {
        try
        {
            string imageUrl = string.Empty;
            string title = string.Empty;

            if (Constants.SupportedSubRegionCountryCodeList.Contains(subRegionCode.CountryCode.ToLower()))
            {
                // Show emblem for supported sub-region
                imageUrl = SubRegion.GetFlagSource(subRegionCode);
                title = $"{AppResources.NewRegion}: {subRegionTitle}";
            }

            // Only show popup if we have an image to display
            if (string.IsNullOrEmpty(imageUrl)) return;

            var popupViewModel = new LocationDiscoveryPopupViewModel(imageUrl, false, title);
            var popup = new LocationDiscoveryPopupView(popupViewModel);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            // If popup fails, don't block the location saving process
#if DEBUG
            Console.WriteLine($"Failed to show location discovery popup: {ex.Message}");
#endif
        }
    }

    public void CancelRequest()
    {
        if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
            _cancelTokenSource.Cancel();
    }

    [ObservableProperty]
    ObservableCollection<PinFilterChip> _filterChips = [];

    // Pin keys (board names / arrival location) currently shown. Empty means all pins are shown.
    readonly HashSet<string> _visiblePinKeys = [];

    [RelayCommand]
    void ToggleFilterChip(PinFilterChip chip)
    {
        var keys = PinFilterRules.Toggle(FilterChips, chip);
        _visiblePinKeys.Clear();
        _visiblePinKeys.UnionWith(keys);
        UpdatePinsVisibility();
    }

    private void OnPinFilterChanged()
    {
        foreach (var chip in FilterChips)
        {
            chip.IsSelected = chip.IsAll ? _visiblePinKeys.Count == 0 : _visiblePinKeys.Contains(chip.Name);
        }
        UpdatePinsVisibility();
    }

    private void UpdatePinsVisibility()
    {
        if (ArrivalMap?.Pins is null) return;
        foreach (var pin in ArrivalMap.Pins)
        {
            // Pins without a tag (e.g. the tapped point) are never filtered out.
            var pinKey = (pin.Tag as MapPinTag)?.PinKey;
            pin.IsVisible = _visiblePinKeys.Count == 0 || pinKey is null || _visiblePinKeys.Contains(pinKey);
        }
    }

    private async Task TryToOfferSubscription()
    {
        var customerInfoResult = await _revenueCat.GetCustomerInfo();
        var isSubscribed = customerInfoResult.IsSuccess && customerInfoResult.Value?.ActiveSubscriptions?.Count > 0;
        _settingsPreferences.SetIsSubscribed(isSubscribed);

        if (_settingsPreferences.GetIsSubscribed()) return;
        await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
    }

    private async Task InitializeMapPins()
    {
        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        AddPinsToMap(arrivalLocationPins);

        var customLocationList = await _customLocationDataRepository.GetAllCustomLocationPins();
        AddPinsToMap(customLocationList);

        await RebuildPinFilterList();
    }

    private async Task ReloadCustomLocationPinsAsync()
    {
        if (ArrivalMap is null) return;

        if (SelectedPin is CustomLocationPin)
        {
            ArrivalMap.SelectedPin = null;
        }

        // Remove by index: Pin equality is value-based (label/position).
        for (int i = ArrivalMap.Pins.Count - 1; i >= 0; i--)
        {
            if (ArrivalMap.Pins[i] is CustomLocationPin)
            {
                ArrivalMap.Pins.RemoveAt(i);
            }
        }

        AddPinsToMap(await _customLocationDataRepository.GetAllCustomLocationPins());
        UpdatePinsVisibility();
    }

    private async Task RebuildPinFilterList()
    {
        var filterList = new List<CustomBoardPinFilterItem>(CustomBoardPinFilterItem.CreateFilterList());

        var hasArrivalPins = ArrivalMap?.Pins.Any(p => (p.Tag as MapPinTag)?.IsArrivalLocation ?? false) ?? false;
        if (hasArrivalPins)
        {
            filterList.Add(new CustomBoardPinFilterItem(AppResources.ArrivalLocation));
        }

        var boardList = await _customBoardRepository.GetAllCustomBoards();
        foreach (var board in boardList)
        {
            filterList.Add(new CustomBoardPinFilterItem(board.Name));
        }

        // Drop keys of filters that no longer exist (e.g. deleted boards).
        var removed = _visiblePinKeys.RemoveWhere(key => !filterList.Any(f => f.Name == key)) > 0;

        FilterChips = new ObservableCollection<PinFilterChip>(PinFilterRules.Build(filterList, _visiblePinKeys));

        if (removed)
        {
            UpdatePinsVisibility();
        }
    }

    private void AddPinsToMap(IEnumerable<Pin> customLocationList)
    {
        foreach (var pin in customLocationList)
        {
            ArrivalMap?.Pins.Add(pin);
        }
    }
}