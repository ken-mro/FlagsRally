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
    private readonly IPendingArrivalRepository _pendingArrivalRepository;
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
    private readonly RegionalFlagsService _regionalFlagsService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectsCustomLocationPin))]
    [NotifyPropertyChangedFor(nameof(HasPinDetails))]
    [NotifyPropertyChangedFor(nameof(HasNoPinDetails))]
    [NotifyPropertyChangedFor(nameof(SelectedPinTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedPinSubtitle))]
    [NotifyPropertyChangedFor(nameof(CanCheckIn))]
    [NotifyPropertyChangedFor(nameof(CanRemoveSelectedPin))]
    [NotifyPropertyChangedFor(nameof(RemoveSelectedPinText))]
    [NotifyPropertyChangedFor(nameof(CanViewStamp))]
    [NotifyPropertyChangedFor(nameof(CanLookUpPending))]
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

    // A long press on the map puts a stamp spot; until someone has done it once, the map says so.
    [ObservableProperty]
    bool _showsLongPressHint;

    [RelayCommand]
    void DismissLongPressHint()
    {
        ShowsLongPressHint = false;
        _settingsPreferences.SetHasSeenLongPressHint();
    }

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
    public bool HasPinDetails => SelectedPin is CustomLocationPin or ArrivalLocationPin or PendingArrivalPin;
    public bool HasNoPinDetails => !HasPinDetails;

    public string SelectedPinTitle => SelectedPin?.Label ?? string.Empty;

    public string SelectedPinSubtitle => SelectedPin switch
    {
        CustomLocationPin pin => $"{((MapPinTag)pin.Tag).BoardName} · {(pin.IsVisited ? pin.Address : AppResources.NotVisited)}",
        ArrivalLocationPin pin => $"{AppResources.ArrivalLocation} · {pin.Address}",
        PendingArrivalPin pin => $"{pin.Address} · {AppResources.PendingStampDetail}",
        _ => string.Empty,
    };

    // Only a place not checked in yet: checking in again would replace the date of the first visit.
    public bool CanCheckIn => SelectedPin is CustomLocationPin { IsVisited: false };

    public bool CanRemoveSelectedPin => SelectedPin is ArrivalLocationPin or PendingArrivalPin or CustomLocationPin { IsVisited: true };

    // A pending stamp can be looked up straight away, besides when the map opens or the connection returns.
    public bool CanLookUpPending => SelectedPin is PendingArrivalPin;

    [RelayCommand]
    Task LookUpPendingAsync() => SelectedPin is PendingArrivalPin pin ? LookUpPendingArrivalsAsync(asked: true, pin.Id) : Task.CompletedTask;

    // Arrivals and checked-in places have a stamp, which can be looked at again from the pin.
    public bool CanViewStamp => SelectedPin is ArrivalLocationPin or CustomLocationPin { IsVisited: true };

    [RelayCommand]
    async Task ViewStampAsync()
    {
        switch (SelectedPin)
        {
            case ArrivalLocationPin pin:
                await ShowArrivalStampPopup(pin.Id);
                break;
            case CustomLocationPin { IsVisited: true } pin:
                await ShowCustomLocationImagePopup(pin.CustomLocationKey, celebrates: false);
                break;
        }
    }

    public string RemoveSelectedPinText => SelectedPin is ArrivalLocationPin or PendingArrivalPin ? AppResources.Delete : AppResources.ResetCheckIn;

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
                FanOutOverlappingPins();
                return;
            }
        }
    }

    // Pins at (nearly) the same spot hide each other even zoomed all the way in; tilt them apart.
    private void FanOutOverlappingPins()
    {
        if (ArrivalMap is null) return;
        PinFanHelper.Apply(ArrivalMap.Pins);
    }

    public Map? ArrivalMap
    {
        get => _arrivalMap;
        set
        {
            SetProperty(ref _arrivalMap, value);
            // The blue dot is turned on once the location may be used (ShowMyLocationLayerIfAllowedAsync):
            // turning it on without the permission makes Google Maps throw and the app close.
            _arrivalMap!.MyLocationEnabled = false;
            // The page draws its own my-location button in the app's style, with the other map buttons.
            _arrivalMap.UiSettings.MyLocationButtonEnabled = false;
            _arrivalMap.UiSettings.CompassEnabled = true;
            _arrivalMap.UiSettings.ScrollGesturesEnabled = true;
            // Google's directions / open-in-Maps buttons would also be hidden under the panels; the
            // pin details and check-in spot cards carry the same two actions instead.
            _arrivalMap.UiSettings.MapToolbarEnabled = false;
            // The built-in zoom buttons would be hidden by the panels that float over the map; the page
            // has its own, which move up above whichever panel is showing.
            _arrivalMap.UiSettings.ZoomControlsEnabled = false;
            _arrivalMap.InfoWindowLongClicked += async (sender, e) => await DeleteOrResetPinAsync(e.Pin);
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

        if (ShowsLongPressHint) DismissLongPressHint();
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
        else if (pin is PendingArrivalPin pendingArrivalPin)
        {
            var deletes = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.ConfirmDelete}\n\n", $"{AppResources.Yes}", $"{AppResources.No}");
            if (!deletes) return;

            if (await _pendingArrivalRepository.DeleteAsync(pendingArrivalPin.Id) != 1)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}\n\n", "OK");
                return;
            }
            RemovePendingPin(pendingArrivalPin);
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

    [RelayCommand]
    private async Task ShowMyLocationAsync()
    {
        var userLocation = await GetLastKnownOrDefaultLocationAsync();
        await ShowMyLocationLayerIfAllowedAsync();
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
        // Asking for the location may have just been allowed.
        await ShowMyLocationLayerIfAllowedAsync();
        if (location is null)
            throw new Exception($"{AppResources.UnableToGetLocation}");

        if (location.IsFromMockProvider)
        {
            throw new FakeLocationException($"{AppResources.FakeLocationDetected}");
        }

        return location;
    }

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, IPendingArrivalRepository pendingArrivalRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences, CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, ArrivalLocationService arrivalLocationService, MapFocusRequest mapFocusRequest, RegionalFlagsService regionalFlagsService)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _pendingArrivalRepository = pendingArrivalRepository;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
        _showsLongPressHint = !settingsPreferences.GetHasSeenLongPressHint();
        _customBoardService = customBoardService;
        _arrivalLocationService = arrivalLocationService;
        _mapFocusRequest = mapFocusRequest;
        _regionalFlagsService = regionalFlagsService;
        PinIcons.Style = settingsPreferences.GetPinStyle();

        WeakReferenceMessenger.Default.Register<CustomBoardsChangedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(async () => await ((LocationPageViewModel)recipient).OnCustomBoardsChanged(message)));
        WeakReferenceMessenger.Default.Register<PinStyleChangedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(((LocationPageViewModel)recipient).RedrawPins));

        // Back online: look up the stamps kept as pending, if the map is what is on screen
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
                MainThread.BeginInvokeOnMainThread(async () => await LookUpPendingArrivalsAsync(asked: false));
        };
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

        await ShowMyLocationLayerIfAllowedAsync();

        var key = _mapFocusRequest.Take();
        if (key is not null)
        {
            await ShowCustomLocationAsync(key);
        }
        else if (!_hasCenteredOnUser && await IsLocationAllowedAsync())
        {
            // Allowed since the map was made (on the how-to pages): show where the user is.
            _hasCenteredOnUser = true;
            var location = await GetLastKnownOrDefaultLocationAsync();
            await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(new Position(location.Latitude, location.Longitude), DEFAULT_ZOOM_LEVEL), animate: false);
        }
    }

    // Whether the map has been moved to the user's location yet.
    bool _hasCenteredOnUser;

    private static async Task<bool> IsLocationAllowedAsync() =>
        await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted;

    private async Task ShowMyLocationLayerIfAllowedAsync()
    {
        if (ArrivalMap is not null && !ArrivalMap.MyLocationEnabled && await IsLocationAllowedAsync())
        {
            ArrivalMap.MyLocationEnabled = true;
        }
    }

    private async Task ShowCustomLocationAsync(string compositeKey)
    {
        if (ArrivalMap is null) return;

        var pin = ArrivalMap.Pins.OfType<CustomLocationPin>().FirstOrDefault(p => p.CustomLocationKey == compositeKey);
        if (pin is null) return;

        // Make sure a filter does not hide the requested pin: show its board again.
        if (!pin.IsVisible && (pin.Tag as MapPinTag)?.PinKey is string key)
        {
            SetHiddenPinKeys(PinFilterRules.SetShown(_hiddenPinKeys, [key], shown: true));
        }

        await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(pin.Position, DEFAULT_ZOOM_LEVEL), animate: true);
        ArrivalMap.SelectedPin = pin;
    }

    private async Task OnCustomBoardsChanged(CustomBoardsChangedMessage message)
    {
        try
        {
            // Boards just added are not among the hidden keys, so they are shown even while filtering.
            if (message.PlacesChanged)
            {
                await ReloadCustomLocationPinsAsync();
            }
            await RebuildPinFilterList();
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

            // The map is made at start-up, under the how-to pages: asking for the location here would
            // put the system's prompt over them. Use it only if it was already allowed.
            _hasCenteredOnUser = await IsLocationAllowedAsync();
            Location location = _hasCenteredOnUser
                ? await GetLastKnownOrDefaultLocationAsync()
                : new Location(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
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
        await LookUpPendingArrivalsAsync(asked: false);
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

            // Stamps kept as pending become arrivals later, so they count towards the free ones
            var arrivalLocationCount = (await _arrivalLocationRepository.GetAllArrivalLocations()).Count + await _pendingArrivalRepository.CountAsync();
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
            var arrivalDate = DateTime.Now;

            // The spot has passed every check; only its country and region need the internet. Without
            // a connection it is kept as it is and looked up later.
            ArrivalLocationData rawArrivalLocationData;
            try
            {
                rawArrivalLocationData = await LookUpArrivalAsync(arrivalDate, currentLocation);
            }
            catch (Exception ex) when (ConnectionFailure.IsConnectionProblem(ex))
            {
                await KeepAsPendingAsync(arrivalDate, currentLocation);
                return;
            }

            var result = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{rawArrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                await RecordArrivalAsync(rawArrivalLocationData);
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

    // Lookups that fail for want of a connection are thrown with no waiting: an offline device
    // would otherwise sit through the request's timeout.
    private async Task<ArrivalLocationData> LookUpArrivalAsync(DateTime arrivalDate, Location location)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            throw new HttpRequestException(AppResources.StillNoConnection);

        string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return await _customGeolocation.GetArrivalLocationAsync(arrivalDate, location, languageCode);
    }

    // Saves a confirmed arrival, puts its pin on the map and presses its stamps down.
    private async Task RecordArrivalAsync(ArrivalLocationData rawArrivalLocationData)
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
        FanOutOverlappingPins();

        // Every stamp is pressed down (the region's too where there are regional flags); only a first
        // country or region gets confetti and "New"
        await ShowStampAcquiredPopup(arrivalLocation, isFirstTimeCountry, subRegionCode is not null, isFirstTimeSupportedSubRegion);

        // A new region's flag also joins the country's regional flags in Collections
        if (isFirstTimeSupportedSubRegion && subRegionCode is not null)
        {
            await ShowCollectionAddedPopup(subRegionCode);
        }
    }

    // The checked spot and time, kept until the country and region can be looked up.
    private async Task KeepAsPendingAsync(DateTime arrivalDate, Location location)
    {
        var pending = await _pendingArrivalRepository.Save(new PendingArrivalData(arrivalDate, location));
        RemoveCheckInSpot();
        ArrivalMap?.Pins.Add(new PendingArrivalPin(pending));
        FanOutOverlappingPins();
        await RebuildPinFilterList();
        await Shell.Current.DisplayAlertAsync($"{AppResources.SavedAsPendingTitle}", $"{AppResources.SavedAsPending}", "OK");
    }

    bool _isLookingUpPending;

    // Pending stamps answered No since the app started: kept, but not asked about again by themselves
    // each time the map opens; Look up now on the pin still asks.
    readonly HashSet<int> _declinedPendingIds = [];

    /// <summary>
    /// Looks up the country and region of the stamps kept as pending, oldest first, and records each
    /// one confirmed as it would have been at the time: its own spot and date, nothing from now.
    /// Runs by itself when the map opens or the connection returns (quietly stopping while still
    /// offline), and when asked from a pending pin for that one (then saying why it could not).
    /// </summary>
    public async Task LookUpPendingArrivalsAsync(bool asked, int? onlyId = null)
    {
        if (_isLookingUpPending || IsBusy || _isCheckingLocation || ArrivalMap is null || !_pinsLoaded) return;
        // Not over another page: the questions it asks belong to the map
        if (!asked && Shell.Current?.CurrentPage is not LocationPage) return;

        try
        {
            _isLookingUpPending = true;
            var pendingArrivals = (await _pendingArrivalRepository.GetAllAsync())
                .Where(p => onlyId is null ? !_declinedPendingIds.Contains(p.Id) : p.Id == onlyId);
            foreach (var pending in pendingArrivals)
            {
                ArrivalLocationData rawArrivalLocationData;
                try
                {
                    rawArrivalLocationData = await LookUpArrivalAsync(pending.ArrivalDate, pending.Location);
                }
                catch (Exception ex) when (ConnectionFailure.IsConnectionProblem(ex))
                {
                    if (asked) await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.StillNoConnection}", "OK");
                    return;
                }

                // The map goes to the stamp first, so the question is asked with the place in view
                var pin = ArrivalMap.Pins.OfType<PendingArrivalPin>().FirstOrDefault(p => p.Id == pending.Id);
                await MoveCameraSafelyAsync(CameraUpdateFactory.NewPositionZoom(new Position(pending.Latitude, pending.Longitude), DEFAULT_ZOOM_LEVEL), animate: true);

                var found = string.Format(AppResources.PendingFound, pending.ArrivalDate.ToString("g", CultureInfo.CurrentCulture));
                var confirms = await Shell.Current.DisplayAlertAsync($"{AppResources.Confirmation}", $"{found}\n{AppResources.PendingIsThisWhere}\n\n" +
                                                            $"{rawArrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");

                // Yes records it (before it is let go, so a failure keeps it); No keeps it pending, to be
                // looked up again or deleted from its pin.
                if (!confirms)
                {
                    _declinedPendingIds.Add(pending.Id);
                    continue;
                }
                await RecordArrivalAsync(rawArrivalLocationData);
                await _pendingArrivalRepository.DeleteAsync(pending.Id);
                _declinedPendingIds.Remove(pending.Id);
                if (pin is not null) RemovePendingPin(pin);
            }
        }
        catch (Exception ex)
        {
            if (asked) await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
        }
        finally
        {
            _isLookingUpPending = false;
        }
    }

    private void RemovePendingPin(PendingArrivalPin pin)
    {
        if (ArrivalMap is not null && ReferenceEquals(ArrivalMap.SelectedPin, pin))
        {
            ArrivalMap.SelectedPin = null;
        }
        RemovePinFromMap(pin);
        _ = RebuildPinFilterList();
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
        await ShowCustomLocationImagePopup(selectedCustomLocationPin.CustomLocationKey, celebrates: true);
    }

    // The board's stamp for the place: with confetti just after checking in, without when looked at again.
    private async Task ShowCustomLocationImagePopup(string customLocationKey, bool celebrates)
    {
        try
        {
            // Get the custom location data to create the popup
            var customLocation = await _customLocationDataRepository.GetCustomLocationByCompositeKey(customLocationKey);
            if (customLocation is null) return;

            var popupViewModel = new CustomLocationImagePopupViewModel(customLocation, celebrates);
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

    private async Task ShowArrivalStampPopup(int arrivalLocationId)
    {
        try
        {
            var arrival = await _arrivalLocationRepository.GetArrivalLocationAsync(arrivalLocationId);
            if (arrival is null)
            {
                await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", $"{AppResources.PleaseTryAgain}", "OK");
                return;
            }

            var popup = new ArrivalStampPopupView(new ArrivalStampPopupViewModel(arrival, _settingsPreferences.GetPassportStampStyle()));
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Failed to show the arrival stamp: {ex.Message}");
#endif
        }
    }

    // After the stamp of a new region: its flag joining the country's card in Collections
    private async Task ShowCollectionAddedPopup(SubRegionCode subRegionCode)
    {
        try
        {
            var country = _regionalFlagsService.GetCountry(subRegionCode.CountryCode);
            var regions = await _regionalFlagsService.GetRegionsAsync(country);
            var region = regions.FirstOrDefault(r => r.Code == subRegionCode && r.HasBeenVisited);
            if (region is null) return;

            var viewModel = new CollectionAddedPopupViewModel(
                region, country.CountryName, regions.Count(r => r.HasBeenVisited), regions.Count);
            await Shell.Current.CurrentPage.ShowPopupAsync(new CollectionAddedPopupView(viewModel));
        }
        catch (Exception ex)
        {
            // If popup fails, don't block the location saving process
#if DEBUG
            Console.WriteLine($"Failed to show the collection: {ex.Message}");
#endif
        }
    }

    // After every stamp (or, as set in Settings, only a new one), in the passport's design: the
    // country's stamp and, where the country has regional flags, the region's, one page after the other
    private async Task ShowStampAcquiredPopup(ArrivalLocation arrivalLocation, bool newCountry, bool hasRegions, bool newRegion)
    {
        try
        {
            var slides = StampAcquiredPopupViewModel.SlidesFor(arrivalLocation, _settingsPreferences.GetPassportStampStyle(), newCountry, hasRegions, newRegion,
                _settingsPreferences.GetShowsEveryStamp());
            if (slides.Count == 0) return;

            var popup = new StampAcquiredPopupView(new StampAcquiredPopupViewModel(slides));
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            // If popup fails, don't block the location saving process
#if DEBUG
            Console.WriteLine($"Failed to show the new stamp: {ex.Message}");
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

    // Pin keys (board names / arrival location) hidden from the map. Empty means all pins are shown.
    readonly HashSet<string> _hiddenPinKeys = [];

    [RelayCommand]
    async Task ToggleFilterChipAsync(PinFilterChip chip)
    {
        if (chip.IsGroup)
        {
            await ChooseSeriesBoardsAsync(chip);
            return;
        }

        SetHiddenPinKeys(PinFilterRules.Toggle(FilterChips, chip, _hiddenPinKeys));
    }

    // One button for Select all and Clear all. While every pin is shown it looks like a selected
    // chip (two white ticks on navy) and a tap clears them all; otherwise it shows a box with a dash
    // (some shown) or an empty box (none shown), and a tap shows them all.
    [RelayCommand]
    void ToggleAllPins() => SetHiddenPinKeys(PinFilterRules.ToggleAll(FilterChips, _hiddenPinKeys));

    public bool AllPinsShown => PinFilterRules.Shown(FilterChips, _hiddenPinKeys) == PinsShown.All;

    public string ToggleAllPinsIcon => PinFilterRules.Shown(FilterChips, _hiddenPinKeys) switch
    {
        PinsShown.All => "all_pins_shown.png",
        PinsShown.Some => "some_pins_shown.png",
        _ => "no_pins_shown.png",
    };

    // Says what a tap does, for screen readers.
    public string ToggleAllPinsDescription => AllPinsShown ? AppResources.ClearBoardSelection : AppResources.SelectAllBoards;

    // A series chip (such as マンホールカード) opens its boards to show all of them or some.
    private Task ChooseSeriesBoardsAsync(PinFilterChip chip) => ChoosePinKeysAsync(chip.Name, chip.Members);

    // Every board and the arrival location in one list, with Select all and Clear selection, so that
    // hiding all of them or all but a few takes a few taps however many boards there are.
    [RelayCommand]
    Task ChooseShownPinsAsync() => ChoosePinKeysAsync(AppResources.PinsToShow, PinFilterRules.AllKeys(FilterChips));

    // A list of pin keys, ticked when their pins are shown; each change applies to the map at once.
    private async Task ChoosePinKeysAsync(string title, IEnumerable<string> keys)
    {
        var items = keys.Select(key => new BoardSeriesFilterItem(key, !_hiddenPinKeys.Contains(key)));
        var viewModel = new BoardSeriesFilterViewModel(title, items, (names, shown) =>
            SetHiddenPinKeys(PinFilterRules.SetShown(_hiddenPinKeys, names, shown)));
        await Shell.Current.CurrentPage.ShowPopupAsync(new BoardSeriesFilterPopupView(viewModel));
    }

    private void OnToggleAllPinsChanged()
    {
        OnPropertyChanged(nameof(AllPinsShown));
        OnPropertyChanged(nameof(ToggleAllPinsIcon));
        OnPropertyChanged(nameof(ToggleAllPinsDescription));
    }

    private void SetHiddenPinKeys(IReadOnlyCollection<string> keys)
    {
        _hiddenPinKeys.Clear();
        _hiddenPinKeys.UnionWith(keys);
        OnPinFilterChanged();
    }

    private void OnPinFilterChanged()
    {
        PinFilterRules.Refresh(FilterChips, _hiddenPinKeys);
        OnToggleAllPinsChanged();
        UpdatePinsVisibility();
    }

    private void UpdatePinsVisibility()
    {
        if (ArrivalMap?.Pins is null) return;
        foreach (var pin in ArrivalMap.Pins)
        {
            // Pins without a tag (e.g. the tapped point) are never filtered out.
            var pinKey = (pin.Tag as MapPinTag)?.PinKey;
            pin.IsVisible = pinKey is null || !_hiddenPinKeys.Contains(pinKey);
        }
        FanOutOverlappingPins();
    }

    private async Task TryToOfferSubscription()
    {
        // Without an answer from RevenueCat (offline, for one), the last known status stands
        var customerInfoResult = await _revenueCat.GetCustomerInfo();
        if (customerInfoResult.IsSuccess)
        {
            _settingsPreferences.SetIsSubscribed(customerInfoResult.Value?.ActiveSubscriptions?.Count > 0);
        }

        if (_settingsPreferences.GetIsSubscribed()) return;
        await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
    }

    private async Task InitializeMapPins()
    {
        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        AddPinsToMap(arrivalLocationPins);
        AddPinsToMap((await _pendingArrivalRepository.GetAllAsync()).Select(p => new PendingArrivalPin(p)));

        var customLocationList = await _customLocationDataRepository.GetAllCustomLocationPins();
        AddPinsToMap(customLocationList);
        FanOutOverlappingPins();

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
        var filterList = new List<CustomBoardPinFilterItem>();

        var hasArrivalPins = ArrivalMap?.Pins.Any(p => p.Tag is MapPinTag { IsArrivalLocation: true } or MapPinTag { IsPendingArrival: true }) ?? false;
        if (hasArrivalPins)
        {
            filterList.Add(new CustomBoardPinFilterItem(AppResources.ArrivalLocation));
        }

        // Boards of one series (the shared folder they came from, or the same name but for the
        // issue number) share a chip, in the place of the series' first board.
        var boardList = await _customBoardRepository.GetAllCustomBoards();
        foreach (var series in boardList.GroupBy(b => BoardSeries.Of(b.Name, b.Folder)))
        {
            var names = series.Select(b => b.Name).ToList();
            filterList.Add(names.Count == 1 ? new CustomBoardPinFilterItem(names[0]) : new CustomBoardPinFilterItem(series.Key, names));
        }

        // Drop keys of filters that no longer exist (e.g. deleted boards).
        var removed = _hiddenPinKeys.RemoveWhere(key => !filterList.Any(f => f.Members.Contains(key))) > 0;

        FilterChips = new ObservableCollection<PinFilterChip>(PinFilterRules.Build(filterList, _hiddenPinKeys));
        OnToggleAllPinsChanged();

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