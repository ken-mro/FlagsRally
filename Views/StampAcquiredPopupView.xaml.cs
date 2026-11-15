using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class StampAcquiredPopupView : Popup
{
    readonly StampAcquiredPopupViewModel _viewModel;
    readonly HashSet<int> _pressed = [];

    public StampAcquiredPopupView(StampAcquiredPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = _viewModel = viewModel;
        slides.PageShownAsync = PressAsync;
        // The first stamp is pressed once the popup is on screen
        slides.Stamp.Opacity = 0;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => _ = PressAsync());
    }

    // The page's stamp pressed down (StampPress), with confetti when it is new; once the last page's
    // has been, OK comes on.
    async Task PressAsync()
    {
        var stamp = slides.Stamp;
        var index = _viewModel.Index;
        if (_pressed.Contains(index))
        {
            stamp.Opacity = 1;
            stamp.Scale = 1;
            stamp.Rotation = 0;
        }
        else
        {
            await StampPress.PressAsync(stamp, _viewModel.Current.Celebrates ? slides.Celebrate : null);
            _pressed.Add(index);
        }
        if (_pressed.Count == _viewModel.Slides.Count) _viewModel.CanClose = true;
    }
}
