using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationPage : ContentPage
{
    // Space between the zoom buttons and the panel below them.
    const double ZoomButtonsGap = 12;
    const uint ZoomButtonsMoveMs = 150;

    readonly LocationPageViewModel _viewModel;

    // Where the map was last left, to put it back if Android rebuilds the map view (the app's screen
    // can be recreated while it is in the background, e.g. after opening directions in Maps).
    Maui.GoogleMaps.CameraPosition? _lastCamera;

	public LocationPage(LocationPageViewModel vm)
	{
		InitializeComponent();
		vm.ArrivalMap = map;
        BindingContext = _viewModel = vm;

        map.CameraIdled += (_, e) => _lastCamera = e.Position;
        map.HandlerChanged += (_, _) => RestoreCamera();

        foreach (var panel in new View[] { getLocationPanel, checkInSpotPanel, pinDetailsPanel })
        {
            panel.SizeChanged += (_, _) => PlaceZoomButtons();
            panel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IsVisible)) PlaceZoomButtons();
            };
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ShowRequestedLocationAsync();
    }

    // A rebuilt map view starts at the map's default camera; move it back to where the user left it.
    void RestoreCamera()
    {
        if (map.Handler is null || _lastCamera is not Maui.GoogleMaps.CameraPosition camera) return;
        Dispatcher.Dispatch(() => map.MoveCamera(Maui.GoogleMaps.CameraUpdateFactory.NewCameraPosition(camera)));
    }

    // The zoom buttons sit at the bottom right like Google's own, just above whichever panel is
    // showing (the Get Location button, the check-in spot guide or the pin details), so a taller
    // panel never covers them. Only the buttons move; the map stays where it is.
    void PlaceZoomButtons()
    {
        var panelHeight = new View[] { getLocationPanel, checkInSpotPanel, pinDetailsPanel }
            .Where(p => p.IsVisible && p.Height > 0)
            .Select(p => p.Height)
            .DefaultIfEmpty(0)
            .Max();

        zoomButtons.CancelAnimations();
        _ = zoomButtons.TranslateToAsync(0, -(panelHeight + ZoomButtonsGap), ZoomButtonsMoveMs, Easing.CubicOut);
    }
}
