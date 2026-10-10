using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class LocationPage : ContentPage
{
    // Space between the zoom buttons and the panel below them.
    const double ZoomButtonsGap = 12;
    const uint ZoomButtonsMoveMs = 150;

    readonly LocationPageViewModel _viewModel;

	public LocationPage(LocationPageViewModel vm)
	{
		InitializeComponent();
		vm.ArrivalMap = map;
        BindingContext = _viewModel = vm;

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
