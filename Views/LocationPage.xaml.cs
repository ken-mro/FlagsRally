using FlagsRally.Helpers;
using FlagsRally.Resources;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationPage : ContentPage
{
    // Space between the map buttons and the panel below them.
    const double MapButtonsGap = 12;
    const uint MapButtonsMoveMs = 150;

    readonly LocationPageViewModel _viewModel;

    // Where the map was last left, to put it back if Android rebuilds the map view (the app's screen
    // can be recreated while it is in the background, e.g. after opening directions in Maps).
    Maui.GoogleMaps.CameraPosition? _lastCamera;

	public LocationPage(LocationPageViewModel vm)
	{
		InitializeComponent();
		vm.ArrivalMap = map;
        ringController.Button = ringButton;
        BindingContext = _viewModel = vm;

        map.CameraIdled += (_, e) =>
        {
            _lastCamera = e.Position;
            ShowZoom(e.Position.Zoom);
        };
        map.CameraChanged += (_, e) => ShowZoom(e.Position.Zoom);
        map.CameraMoveStarted += (_, e) =>
        {
            if (e.IsGesture) CloseRingController();
        };
        map.MapClicked += (_, _) => CloseRingController();
        map.MapLongClicked += (_, _) => CloseRingController();
        map.PinClicked += (_, _) => CloseRingController();
        map.HandlerChanged += (_, _) =>
        {
            RestoreCamera();
            PadMapBelowChips();
        };

        filterChips.SizeChanged += (_, _) => PadMapBelowChips();

        foreach (var panel in new View[] { getLocationPanel, checkInSpotPanel, pinDetailsPanel })
        {
            panel.SizeChanged += (_, _) => PlaceMapButtons();
            panel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IsVisible)) PlaceMapButtons();
            };
        }
    }

    void OnRingStarted(object? sender, EventArgs e) => _viewModel.BeginPadMove();

    void OnRingMoved(object? sender, Controls.MapRingController.MoveEventArgs e) =>
        _viewModel.PadMove(e.PanX, e.PanY, e.ZoomChange);

    async void OnRingResetRequested(object? sender, EventArgs e) => await _viewModel.ResetCameraAsync();

    void OnRingOpenChanged(object? sender, bool open)
    {
        ringStatus.IsVisible = open;
        if (open) ShowRingStatus();
    }

    void OnRingStateChanged(object? sender, EventArgs e) => ShowRingStatus();

    // The map was touched outside the controller (a tap, a long press, a pin, or a drag or pinch,
    // which the map still does): the controller closes, unless a thumb is on it.
    void CloseRingController() => ringController.Close();

    void ShowZoom(double zoom)
    {
        ringController.ShowZoom(zoom, LocationPageViewModel.HomeZoom);
        ringScale.Text = MapRing.FormatMagnification(MapRing.Magnification(zoom, LocationPageViewModel.HomeZoom));
    }

    void ShowRingStatus()
    {
        ShowZoom(map.CameraPosition?.Zoom ?? LocationPageViewModel.HomeZoom);
        var speed = ringController.ZoomSpeed;
        (ringState.Text, ringState.TextColor) = ringController.CurrentState switch
        {
            Controls.MapRingController.State.Moving => (AppResources.MapRingMoving, AppColors.Get("Navy", "#182A52")),
            Controls.MapRingController.State.Zooming => (
                speed > 0 ? string.Format(AppResources.MapRingZoomIn, MapRing.FormatRate(speed))
                : speed < 0 ? string.Format(AppResources.MapRingZoomOut, MapRing.FormatRate(speed))
                : AppResources.MapRingZoomStopped,
                AppColors.Get("Stamp", "#B94047")),
            _ => (AppResources.MapRingIdle, AppColors.Get("InkMuted", "#7A7262")),
        };
    }

    void OnMapButtonPressed(object? sender, EventArgs e) => ringController.Close();

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Leaving the tab closes the controller, even with a thumb on it.
        ringController.Close(evenWhileHeld: true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ShowRequestedLocationAsync();
        await _viewModel.LookUpPendingArrivalsAsync(asked: false);
    }

    // A rebuilt map view starts at the map's default camera; move it back to where the user left it.
    void RestoreCamera()
    {
        if (map.Handler is null || _lastCamera is not Maui.GoogleMaps.CameraPosition camera) return;
        Dispatcher.Dispatch(() => _ = map.MoveCameraSafelyAsync(Maui.GoogleMaps.CameraUpdateFactory.NewCameraPosition(camera), animate: false));
    }

    // Google draws its compass in the map's top corner (left on Android, right on iOS), where the
    // filter chips are. Padding the map's top edge by the chips' height moves the compass below them.
    // The library applies Map.Padding only when the map is created, so the native map is padded too.
    void PadMapBelowChips()
    {
        if (filterChips.Height <= 0) return;
        var top = filterChips.Margin.Top + filterChips.Height + MapButtonsGap / 2;
        ringStatus.Margin = new Thickness(ringStatus.Margin.Left, top, ringStatus.Margin.Right, 0);
        var padding = new Thickness(map.Padding.Left, top, map.Padding.Right, map.Padding.Bottom);
        map.Padding = padding;
#if ANDROID
        if (map.Handler?.PlatformView is Android.Gms.Maps.MapView mapView)
        {
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            mapView.GetMapAsync(new MapReadyCallback(nativeMap => nativeMap.SetPadding(
                (int)(padding.Left * density), (int)(padding.Top * density),
                (int)(padding.Right * density), (int)(padding.Bottom * density))));
        }
#elif IOS
        if (map.Handler?.PlatformView is Google.Maps.MapView nativeMap)
        {
            nativeMap.Padding = new UIKit.UIEdgeInsets((nfloat)padding.Top, (nfloat)padding.Left,
                (nfloat)padding.Bottom, (nfloat)padding.Right);
        }
#endif
    }

    // The map controls (the ring controller, directions, open in Maps, my location) sit at the bottom right like Google's own,
    // just above whichever panel is showing (the Get Location button, the check-in spot guide or
    // the pin details), so a taller panel never covers them. Only the buttons move; the map stays where it is.
    void PlaceMapButtons()
    {
        var panelHeight = new View[] { getLocationPanel, checkInSpotPanel, pinDetailsPanel }
            .Where(p => p.IsVisible && p.Height > 0)
            .Select(p => p.Height)
            .DefaultIfEmpty(0)
            .Max();

        mapButtons.CancelAnimations();
        _ = mapButtons.TranslateToAsync(0, -(panelHeight + MapButtonsGap), MapButtonsMoveMs, Easing.CubicOut);
    }

#if ANDROID
    sealed class MapReadyCallback(Action<Android.Gms.Maps.GoogleMap> onReady) : Java.Lang.Object, Android.Gms.Maps.IOnMapReadyCallback
    {
        public void OnMapReady(Android.Gms.Maps.GoogleMap googleMap) => onReady(googleMap);
    }
#endif
}
